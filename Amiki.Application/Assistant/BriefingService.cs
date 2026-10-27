using Amiki.Contracts.Assistant;
using Amiki.Application.People;
using Amiki.Application.Data;
using System.Globalization;
using System.Text;
using Amiki.Domain;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;
using Amiki.Domain.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant;

/// <summary>
/// The dashboard's "from Amiki" card: fresh nudges every time, plus her short note for the day,
/// written by the model once a day and saved. Only areas she's allowed to see go in; never the
/// diary. If the model is off or busy, the nudges still show.
/// </summary>
public sealed class BriefingService(IAmikiDb db, ILanguageModel model, ICurrentUser user, BriefingGate gate)
{
    public async Task<Briefing> GetAsync(DateTime now, bool rewrite, CancellationToken ct)
    {
        var settings = await db.AssistantSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new AssistantSettings();
        var input = await GatherAsync(settings, now.Date, ct);
        var nudges = NudgeRules.From(input);

        if (!model.IsEnabled && !user.IsViewingSomeoneElse) return new Briefing(null, null, nudges);

        var saved = await SavedAsync(now, ct);
        if (saved is not null && !rewrite) return new Briefing(saved.Text, saved.WrittenAt, nudges);
        // Viewing someone else's Amiki is look-only: their saved note if there is one, never a new one.
        if (user.IsViewingSomeoneElse) return new Briefing(saved?.Text, saved?.WrittenAt, nudges);
        if (gate.IsPaused(user.DataUserId))
            return new Briefing(saved?.Text, saved?.WrittenAt, nudges, "Gemini's free-tier limit was reached; she'll write it in a few minutes.");

        using var turn = await gate.WaitToWriteAsync(user.DataUserId, ct);
        try
        {
            if (!rewrite && await SavedAsync(now, ct) is { } justWritten) return new Briefing(justWritten.Text, justWritten.WrittenAt, nudges);
            var text = await model.WriteAsync(Instructions(settings, now), Facts(input, nudges), ct);
            var briefing = DailyBriefing.For(now.Date, text, now);
            await SaveAsync(briefing, ct);
            return new Briefing(briefing.Text, briefing.WrittenAt, nudges);
        }
        catch (LanguageModelException ex)
        {
            if (ex.RateLimited) gate.PauseAfterRateLimit(user.DataUserId);
            return new Briefing(saved?.Text, saved?.WrittenAt, nudges, ex.Message);
        }
    }

    private Task<DailyBriefing?> SavedAsync(DateTime now, CancellationToken ct) =>
        db.DailyBriefings.AsNoTracking().FirstOrDefaultAsync(b => b.Day == now.Date, ct);

    // Replaces today's note; if another device saved one at the same moment, the newest wins.
    private async Task SaveAsync(DailyBriefing briefing, CancellationToken ct)
    {
        var existing = await db.DailyBriefings.FirstOrDefaultAsync(b => b.Id == briefing.Id, ct);
        if (existing is null) db.DailyBriefings.Add(briefing);
        else db.Entry(existing).CurrentValues.SetValues(briefing);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { /* the other device's note is just as good */ }
    }

    internal static string Instructions(AssistantSettings settings, DateTime now)
    {
        var s = new StringBuilder();
        s.AppendLine("You are Amiki, the user's personal assistant. Write their short note for today, shown at the top of their dashboard.");
        s.AppendLine($"Personality: {(string.IsNullOrWhiteSpace(settings.Personality) ? AssistantPrompt.DefaultPersonality : settings.Personality.Trim())}");
        s.AppendLine($"Now: {now.ToString("dddd, MMMM d, h:mm tt", CultureInfo.InvariantCulture)}.");
        s.AppendLine("Write 2 to 4 short lines in plain text (no markdown, no bullet lists): a greeting that fits the time of day, the one or two things that matter most today, and a little encouragement.");
        s.AppendLine("Use only the facts given; don't invent tasks, amounts or events. If there's little going on, keep it light. Match the user's language.");
        if (!string.IsNullOrWhiteSpace(settings.AboutMe))
        {
            s.AppendLine("About the user, in their own words:");
            s.AppendLine(settings.AboutMe.Trim());
        }
        return s.ToString();
    }

    internal static string Facts(NudgeInput d, IReadOnlyList<Nudge> nudges)
    {
        var s = new StringBuilder("Today's facts:\n");
        if (d.SeeTasks)
        {
            var today = d.Today.Date;
            s.AppendLine($"- Open tasks: {d.OpenTasks.Count}. Due today: {Titles(d.OpenTasks.Where(t => t.Due?.Date == today))}. Due tomorrow: {Titles(d.OpenTasks.Where(t => t.Due?.Date == today.AddDays(1)))}.");
        }
        if (d.SeeMoney)
        {
            var weekStart = WeekRange.Of(d.Today).Start;
            s.AppendLine($"- Spent so far this week: ₱{d.SpendingByDay.Where(x => x.Day >= weekStart).Sum(x => x.Amount):#,0}.");
        }
        foreach (var nudge in nudges) s.AppendLine($"- {nudge.Text}");
        if (nudges.Count == 0) s.AppendLine("- Nothing needs attention.");
        return s.ToString();

        static string Titles(IEnumerable<(string Title, DateTime? Due)> tasks)
        {
            var list = tasks.Select(t => $"“{t.Title}”").Take(5).ToList();
            return list.Count == 0 ? "none" : string.Join(", ", list);
        }
    }

    private async Task<NudgeInput> GatherAsync(AssistantSettings s, DateTime today, CancellationToken ct)
    {
        var since = today.AddDays(-35);
        var tasks = s.SeeTasks
            ? (await db.Tasks.AsNoTracking().Where(t => t.CompletedAt == null).Select(t => new { t.Title, t.Due }).ToListAsync(ct))
                .Select(t => (t.Title, t.Due)).ToList()
            : [];
        var ideas = s.SeeIdeas
            ? await db.Ideas.AsNoTracking().Where(i => i.Status == IdeaStatus.Parked && i.ArchivedAt == null).Select(i => i.CreatedAt).ToListAsync(ct)
            : [];

        List<(string, DateTime?)> accounts = [];
        List<(DateTime, string, decimal)> spending = [];
        if (s.SeeMoney)
        {
            var used = await db.Transactions.AsNoTracking().Select(t => t.Account).Distinct().ToListAsync(ct);
            var checks = await db.BalanceChecks.AsNoTracking().GroupBy(c => c.Account).Select(g => new { Account = g.Key, Last = g.Max(c => c.CheckedAt) }).ToListAsync(ct);
            accounts = used.Order().Select(a => (a, checks.FirstOrDefault(c => c.Account == a)?.Last)).ToList();
            spending = (await db.Transactions.AsNoTracking()
                    .Where(t => t.Kind == TxKind.Expense && t.Date >= since && t.Category != Category.BalanceFix)
                    .Select(t => new { t.Date, t.Category, t.Amount }).ToListAsync(ct))
                .Select(t => (t.Date.Date, t.Category, t.Amount)).ToList();
        }

        var inbox = s.SeeInbox ? await db.Inbox.CountAsync(ct) : 0;
        var reviewed = s.SeeWeeklyNotes
            ? await db.WeeklyNotes.AsNoTracking().Where(n => n.WeekStart >= since && (n.WentWell != "" || n.ToChange != "")).Select(n => n.WeekStart).ToListAsync(ct)
            : [];

        return new NudgeInput(today, tasks, ideas, accounts, spending, inbox, reviewed,
            s.SeeTasks, s.SeeIdeas, s.SeeMoney, s.SeeInbox, s.SeeWeeklyNotes);
    }
}
