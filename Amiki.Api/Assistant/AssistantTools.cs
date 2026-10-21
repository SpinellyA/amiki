using System.Globalization;
using System.Text.Json.Nodes;
using Amiki.Api.Data;
using Amiki.Modules.Assistant;
using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Assistant;

/// <summary>
/// The lookups Amiki can do while chatting. All read-only, all capped in size, and each one only
/// offered if its area is switched on in her settings. There is no diary tool, by design.
/// </summary>
public sealed class AssistantTools(AmikiDb db)
{
    private const int MaxRows = 40;

    /// <summary>The tools for the areas she's allowed to see.</summary>
    public IReadOnlyList<ToolDefinition> For(AssistantSettings settings)
    {
        var tools = new List<ToolDefinition>();
        if (settings.SeeTasks) tools.Add(ListTasks);
        if (settings.SeeIdeas) tools.Add(ListIdeas);
        if (settings.SeeMoney) tools.AddRange([AccountBalances, MoneySummary, ListTransactions]);
        if (settings.SeeInbox) tools.Add(ListInbox);
        if (settings.SeeWeeklyNotes) tools.Add(WeeklyNotes);
        return tools;
    }

    /// <summary>Runs a tool the model asked for. Only tools in <paramref name="allowed"/> run.</summary>
    public async Task<JsonNode> RunAsync(ToolCall call, IReadOnlyList<ToolDefinition> allowed, DateTime today, CancellationToken ct)
    {
        if (allowed.All(t => t.Name != call.Name)) throw new ToolException($"There's no tool called {call.Name} available.");
        return call.Name switch
        {
            "list_tasks" => await TasksAsync(call.Args, today, ct),
            "list_ideas" => await IdeasAsync(call.Args, ct),
            "account_balances" => await BalancesAsync(ct),
            "money_summary" => await SummaryAsync(call.Args, today, ct),
            "list_transactions" => await TransactionsAsync(call.Args, today, ct),
            "list_inbox" => await InboxAsync(ct),
            "weekly_notes" => await NotesAsync(call.Args, ct),
            _ => throw new ToolException($"Unknown tool {call.Name}."),
        };
    }

    // ---------- Definitions ----------

    private static readonly ToolDefinition ListTasks = new("list_tasks",
        "Lists the user's tasks. Use for questions about to-dos, deadlines, what's due, overdue or recently finished.",
        Schema("""
            {"type":"object","properties":{
              "view":{"type":"string","enum":["due_today","overdue","upcoming","open","done_recently"],"description":"due_today, overdue, upcoming (due after today), open (all unfinished), done_recently (finished in the last 14 days)"},
              "project":{"type":"string","description":"Only tasks with this project tag, without #"}},
             "required":["view"]}
            """), "tasks");

    private static readonly ToolDefinition ListIdeas = new("list_ideas",
        "Lists the user's ideas with their status (parked, in progress, done).",
        Schema("""
            {"type":"object","properties":{
              "status":{"type":"string","enum":["parked","in_progress","done","any"]},
              "search":{"type":"string","description":"Only ideas containing this text or tag"},
              "include_archived":{"type":"boolean"}}}
            """), "ideas");

    private static readonly ToolDefinition AccountBalances = new("account_balances",
        "Current balance of each money account (Philippine pesos) and when it was last checked against the real account.",
        null, "money");

    private static readonly ToolDefinition MoneySummary = new("money_summary",
        "Totals for a date range: spent, received, and spending/income per category. Moving money between own accounts isn't counted; transfer fees are.",
        Schema("""
            {"type":"object","properties":{
              "from":{"type":"string","description":"yyyy-MM-dd, inclusive"},
              "to":{"type":"string","description":"yyyy-MM-dd, inclusive"}},
             "required":["from","to"]}
            """), "money");

    private static readonly ToolDefinition ListTransactions = new("list_transactions",
        "Individual transactions in a date range, newest first, optionally filtered by category or note text.",
        Schema("""
            {"type":"object","properties":{
              "from":{"type":"string","description":"yyyy-MM-dd, inclusive"},
              "to":{"type":"string","description":"yyyy-MM-dd, inclusive"},
              "category":{"type":"string"},
              "search":{"type":"string","description":"Text in the note"}},
             "required":["from","to"]}
            """), "money");

    private static readonly ToolDefinition ListInbox = new("list_inbox",
        "Notes the user jotted down quickly and hasn't sorted yet.",
        null, "inbox");

    private static readonly ToolDefinition WeeklyNotes = new("weekly_notes",
        "The user's weekly reflections: what went well and what to change, newest week first.",
        Schema("""{"type":"object","properties":{"weeks":{"type":"integer","description":"How many recent weeks, 1-12"}}}"""), "weekly notes");

    private static JsonObject Schema(string json) => (JsonObject)JsonNode.Parse(json)!;

    // ---------- Lookups ----------

    private async Task<JsonNode> TasksAsync(JsonObject args, DateTime today, CancellationToken ct)
    {
        var view = Str(args, "view") ?? "open";
        var project = Str(args, "project")?.TrimStart('#').ToLowerInvariant();
        var tomorrow = today.AddDays(1);
        var recent = today.AddDays(-14);

        var query = db.Tasks.AsNoTracking();
        if (project is not null) query = query.Where(t => t.Project == project);
        query = view switch
        {
            "due_today" => query.Where(t => t.CompletedAt == null && t.Due >= today && t.Due < tomorrow),
            "overdue" => query.Where(t => t.CompletedAt == null && t.Due < today),
            "upcoming" => query.Where(t => t.CompletedAt == null && t.Due >= tomorrow),
            "done_recently" => query.Where(t => t.CompletedAt >= recent),
            _ => query.Where(t => t.CompletedAt == null),
        };
        var tasks = await query.OrderBy(t => t.Due == null).ThenBy(t => t.Due).Take(MaxRows).ToListAsync(ct);
        return Rows(tasks.Select(t => new JsonObject
        {
            ["title"] = t.Title,
            ["due"] = Day(t.Due),
            ["project"] = t.Project,
            ["priority"] = t.Priority.ToString().ToLowerInvariant(),
            ["done"] = t.CompletedAt is { } done ? Day(done) : null,
            ["from_an_idea"] = t.IdeaId is not null,
        }));
    }

    private async Task<JsonNode> IdeasAsync(JsonObject args, CancellationToken ct)
    {
        var status = Str(args, "status") ?? "any";
        var search = Str(args, "search");
        var archived = args["include_archived"]?.GetValue<bool>() == true;

        var ideas = await db.Ideas.AsNoTracking().OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        var shown = ideas
            .Where(i => archived || i.ArchivedAt is null)
            .Where(i => status switch
            {
                "parked" => i.Status == IdeaStatus.Parked,
                "in_progress" => i.Status == IdeaStatus.InProgress,
                "done" => i.Status == IdeaStatus.Done,
                _ => true,
            })
            .Where(i => search is null || i.ToEditableText().Contains(search, StringComparison.OrdinalIgnoreCase))
            .Take(MaxRows);
        return Rows(shown.Select(i => new JsonObject
        {
            ["text"] = i.Text.Length > 300 ? i.Text[..300] + "…" : i.Text,
            ["tags"] = new JsonArray(i.Tags.Select(t => (JsonNode)JsonValue.Create(t)!).ToArray()),
            ["status"] = i.Status switch { IdeaStatus.InProgress => "in progress", _ => i.Status.ToString().ToLowerInvariant() },
            ["captured"] = Day(i.CreatedAt),
            ["archived"] = i.ArchivedAt is not null,
        }));
    }

    private async Task<JsonNode> BalancesAsync(CancellationToken ct)
    {
        var accounts = await db.Accounts.AsNoTracking().ToListAsync(ct);
        // One person's data is small: add it up here, the same way the Money page does.
        var transactions = await db.Transactions.AsNoTracking().Select(t => new { t.Account, t.Kind, t.Amount }).ToListAsync(ct);
        var transfers = await db.Transfers.AsNoTracking().ToListAsync(ct);
        var checks = await db.BalanceChecks.AsNoTracking().Select(c => new { c.Account, c.CheckedAt }).ToListAsync(ct);

        return Rows(accounts.OrderBy(a => a.Name).Select(a =>
        {
            var balance = a.OpeningBalance
                + transactions.Where(t => t.Account == a.Name).Sum(t => t.Kind == TxKind.Income ? t.Amount : -t.Amount)
                + transfers.Where(t => t.To == a.Name).Sum(t => t.Amount)
                - transfers.Where(t => t.From == a.Name).Sum(t => t.Amount + t.Fee);
            var lastCheck = checks.Where(c => c.Account == a.Name).Select(c => (DateTime?)c.CheckedAt).Max();
            return new JsonObject
            {
                ["account"] = a.Name,
                ["balance"] = balance,
                ["last_checked"] = lastCheck is { } at ? Day(at) : "never",
            };
        }));
    }

    private async Task<JsonNode> SummaryAsync(JsonObject args, DateTime today, CancellationToken ct)
    {
        var (from, to) = Range(args, today);
        var flows = await FlowsAsync(from, to, ct);
        var spending = flows.Where(f => f.Kind == TxKind.Expense).ToList();
        var income = flows.Where(f => f.Kind == TxKind.Income).ToList();
        return new JsonObject
        {
            ["from"] = Day(from),
            ["to"] = Day(to.AddDays(-1)),
            ["spent"] = spending.Sum(f => f.Amount),
            ["received"] = income.Sum(f => f.Amount),
            ["spending_by_category"] = ByCategory(spending),
            ["income_by_category"] = ByCategory(income),
        };
    }

    private async Task<JsonNode> TransactionsAsync(JsonObject args, DateTime today, CancellationToken ct)
    {
        var (from, to) = Range(args, today);
        var category = Str(args, "category");
        var search = Str(args, "search");
        var rows = (await FlowsAsync(from, to, ct))
            .Where(f => category is null || string.Equals(f.Category, category, StringComparison.OrdinalIgnoreCase))
            .Where(f => search is null || f.Note.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.Date)
            .Take(MaxRows);
        return Rows(rows.Select(f => new JsonObject
        {
            ["date"] = Day(f.Date),
            ["kind"] = f.Kind == TxKind.Income ? "received" : "spent",
            ["amount"] = f.Amount,
            ["category"] = f.Category,
            ["account"] = f.Account,
            ["note"] = f.Note,
        }));
    }

    private async Task<JsonNode> InboxAsync(CancellationToken ct)
    {
        var items = await db.Inbox.AsNoTracking().OrderByDescending(i => i.CapturedAt).Take(MaxRows).ToListAsync(ct);
        return Rows(items.Select(i => new JsonObject { ["text"] = i.Text, ["captured"] = Day(i.CapturedAt) }));
    }

    private async Task<JsonNode> NotesAsync(JsonObject args, CancellationToken ct)
    {
        var weeks = Math.Clamp(args["weeks"]?.GetValue<int>() ?? 4, 1, 12);
        var notes = await db.WeeklyNotes.AsNoTracking().OrderByDescending(n => n.WeekStart).Take(weeks).ToListAsync(ct);
        return Rows(notes.Select(n => new JsonObject
        {
            ["week_of"] = Day(n.WeekStart),
            ["went_well"] = n.WentWell,
            ["to_change"] = n.ToChange,
        }));
    }

    // ---------- Helpers ----------

    private sealed record Flow(DateTime Date, TxKind Kind, decimal Amount, string Category, string Account, string Note);

    // Real money in and out, like the Money page: no balance fixes; transfer fees count as spending.
    private async Task<List<Flow>> FlowsAsync(DateTime from, DateTime to, CancellationToken ct)
    {
        var transactions = await db.Transactions.AsNoTracking()
            .Where(t => t.Date >= from && t.Date < to && t.Category != Category.BalanceFix)
            .Select(t => new Flow(t.Date, t.Kind, t.Amount, t.Category, t.Account, t.Note)).ToListAsync(ct);
        var fees = await db.Transfers.AsNoTracking()
            .Where(t => t.Date >= from && t.Date < to && t.Fee > 0)
            .Select(t => new Flow(t.Date, TxKind.Expense, t.Fee, Category.Fees, t.From, "Transfer fee: " + t.From + " to " + t.To)).ToListAsync(ct);
        return [.. transactions, .. fees];
    }

    private static JsonObject ByCategory(IEnumerable<Flow> flows) => new(flows
        .GroupBy(f => f.Category)
        .OrderByDescending(g => g.Sum(f => f.Amount))
        .Select(g => KeyValuePair.Create(g.Key, (JsonNode?)JsonValue.Create(g.Sum(f => f.Amount)))));

    /// <summary>The from/to arguments as [from, day after to). Defaults to this month; at most about a year.</summary>
    internal static (DateTime From, DateTime To) Range(JsonObject args, DateTime today)
    {
        var from = Date(args, "from") ?? new DateTime(today.Year, today.Month, 1);
        var to = (Date(args, "to") ?? today).AddDays(1);
        if (to <= from) throw new ToolException("'to' must be on or after 'from'.");
        if ((to - from).TotalDays > 400) throw new ToolException("That range is too long; ask for a year or less.");
        return (from, to);
    }

    private static DateTime? Date(JsonObject args, string key) =>
        Str(args, key) is { } text
            ? DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : throw new ToolException($"'{key}' must be a date like 2026-10-21.")
            : null;

    private static string? Str(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;

    private static string? Day(DateTime? date) => date?.ToString("yyyy-MM-dd (ddd)", CultureInfo.InvariantCulture);

    private static JsonObject Rows(IEnumerable<JsonObject> rows)
    {
        var list = rows.ToList();
        return new JsonObject
        {
            ["count"] = list.Count,
            ["items"] = new JsonArray(list.Cast<JsonNode>().ToArray()),
            ["note"] = list.Count >= MaxRows ? $"Only the first {MaxRows} are shown." : null,
        };
    }
}
