using Amiki.Application.Data;
using System.Globalization;
using System.Text.Json.Nodes;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;
using Amiki.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant;

/// <summary>
/// The changes Amiki can suggest in chat. These tools never write anything: they check the
/// suggestion against your real data (accounts, categories, which task you mean) and return it
/// as a proposal, which the app shows as a card. Only your "Do it" makes it happen.
/// </summary>
public sealed class AssistantProposals(IAmikiDb db)
{
    public const string Prefix = "propose_";

    /// <summary>The suggestion tools allowed by your settings: only for areas she can see, and only if suggesting is on.</summary>
    public IReadOnlyList<ToolDefinition> For(AssistantSettings settings, bool canRemember = true)
    {
        if (!settings.CanPropose) return [];
        var tools = new List<ToolDefinition>();
        if (settings.SeeTasks) tools.AddRange([AddTask, CompleteTask]);
        if (settings.SeeMoney) tools.Add(LogMoney);
        if (settings.SeeIdeas) tools.Add(AddIdea);
        if (settings.SeeInbox) tools.Add(InboxNote);
        if (canRemember) tools.Add(Remember);
        return tools;
    }

    public async Task<AssistantProposal> ProposeAsync(ToolCall call, DateTime today, CancellationToken ct) => call.Name switch
    {
        "propose_add_task" => TaskProposal(call.Args, today),
        "propose_log_money" => await MoneyProposalAsync(call.Args, today, ct),
        "propose_add_idea" => IdeaProposal(call.Args),
        "propose_complete_task" => await CompleteProposalAsync(call.Args, ct),
        "propose_inbox_note" => new AssistantProposal(Guid.NewGuid(), ProposalKind.AddInboxNote, Required(call.Args, "text", 2000)),
        "propose_remember" => new AssistantProposal(Guid.NewGuid(), ProposalKind.Remember, Required(call.Args, "fact", MaxFact)),
        _ => throw new ToolException($"Unknown tool {call.Name}."),
    };

    /// <summary>What the model is told after suggesting: it's a card now, not a done deal.</summary>
    public static JsonObject Result(AssistantProposal proposal) => new()
    {
        ["proposed"] = proposal.Describe(),
        ["status"] = "Shown to the user as a card. It only happens if they tap 'Do it'; don't say it's done.",
    };

    // ---------- Definitions ----------

    private static readonly ToolDefinition AddTask = new("propose_add_task",
        "Suggest adding a task. The user confirms it in the app; nothing changes until then.",
        Schema("""
            {"type":"object","properties":{
              "title":{"type":"string"},
              "due":{"type":"string","description":"yyyy-MM-dd; omit for today"},
              "project":{"type":"string","description":"A project tag without #"},
              "priority":{"type":"string","enum":["low","medium","high"]}},
             "required":["title"]}
            """), "tasks");

    private static readonly ToolDefinition CompleteTask = new("propose_complete_task",
        "Suggest marking one of the user's open tasks as done. Name it by words from its title.",
        Schema("""{"type":"object","properties":{"task":{"type":"string","description":"Words from the task's title"}},"required":["task"]}"""), "tasks");

    private static readonly ToolDefinition LogMoney = new("propose_log_money",
        "Suggest logging money spent or received (pesos). The user confirms it in the app.",
        Schema("""
            {"type":"object","properties":{
              "kind":{"type":"string","enum":["spent","received"]},
              "amount":{"type":"number"},
              "note":{"type":"string","description":"What it was for, e.g. 'Gas'"},
              "category":{"type":"string","description":"One of the user's categories"},
              "account":{"type":"string","description":"One of the user's accounts; Cash if unclear"},
              "date":{"type":"string","description":"yyyy-MM-dd; omit for today"}},
             "required":["kind","amount","note"]}
            """), "money");

    private static readonly ToolDefinition AddIdea = new("propose_add_idea",
        "Suggest saving an idea for later.",
        Schema("""
            {"type":"object","properties":{
              "text":{"type":"string"},
              "tags":{"type":"array","items":{"type":"string"},"description":"Tags without #"}},
             "required":["text"]}
            """), "ideas");

    private static readonly ToolDefinition InboxNote = new("propose_inbox_note",
        "Suggest jotting something into the Inbox to sort later (when it's unclear what it is).",
        Schema("""{"type":"object","properties":{"text":{"type":"string"}},"required":["text"]}"""), "inbox");

    private const int MaxFact = 300;

    private static readonly ToolDefinition Remember = new("propose_remember",
        "Suggest remembering a lasting fact about the user (added to 'About you' in your settings), e.g. their sem end date or a preference. Use when they ask you to remember something, or share something clearly worth keeping.",
        Schema("""{"type":"object","properties":{"fact":{"type":"string","description":"One short sentence, written about the user, e.g. 'Sem ends Dec 12.'"}},"required":["fact"]}"""), "memory");

    private static JsonObject Schema(string json) => (JsonObject)JsonNode.Parse(json)!;

    // ---------- Checking suggestions ----------

    private static AssistantProposal TaskProposal(JsonObject args, DateTime today) => new(
        Guid.NewGuid(), ProposalKind.AddTask,
        Required(args, "title", 500),
        Due: Date(args, "due") ?? today,
        Project: Tag(Str(args, "project")),
        Priority: Str(args, "priority") switch { "low" => Priority.Low, "medium" => Priority.Medium, "high" => Priority.High, _ => Priority.None });

    private async Task<AssistantProposal> MoneyProposalAsync(JsonObject args, DateTime today, CancellationToken ct)
    {
        var kind = Str(args, "kind") == "received" ? TxKind.Income : TxKind.Expense;
        var amount = args["amount"] is JsonValue v && v.TryGetValue<decimal>(out var a) ? Math.Round(a, 2) : 0;
        if (amount <= 0) throw new ToolException("'amount' must be more than zero.");

        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.Kind == kind && c.Name != Category.BalanceFix && c.Name != Category.Fees)
            .Select(c => c.Name).ToListAsync(ct);
        var accounts = await db.Accounts.AsNoTracking().Select(a => a.Name).ToListAsync(ct);

        return new AssistantProposal(Guid.NewGuid(), ProposalKind.LogMoney,
            Required(args, "note", 500),
            MoneyKind: kind,
            Amount: amount,
            Category: Match(Str(args, "category"), categories) ?? Category.FallbackFor(kind),
            Account: Match(Str(args, "account"), accounts) ?? Match("Cash", accounts) ?? accounts.FirstOrDefault(),
            Date: Date(args, "date") ?? today);
    }

    private static AssistantProposal IdeaProposal(JsonObject args)
    {
        var tags = args["tags"] is JsonArray array
            ? array.Select(t => t is JsonValue v && v.TryGetValue<string>(out var s) ? Tag(s) : null).OfType<string>().Distinct().ToList()
            : [];
        return new AssistantProposal(Guid.NewGuid(), ProposalKind.AddIdea, Required(args, "text", 5000), Tags: tags);
    }

    // Finds the open task meant. Several close matches go back to her, so she asks you which one.
    private async Task<AssistantProposal> CompleteProposalAsync(JsonObject args, CancellationToken ct)
    {
        var words = Required(args, "task", 500);
        var open = await db.Tasks.AsNoTracking().Where(t => t.CompletedAt == null).Select(t => new { t.Id, t.Title }).ToListAsync(ct);
        var exact = open.Where(t => string.Equals(t.Title, words, StringComparison.OrdinalIgnoreCase)).ToList();
        var matches = exact.Count > 0 ? exact : open.Where(t => t.Title.Contains(words, StringComparison.OrdinalIgnoreCase)).ToList();

        return matches.Count switch
        {
            1 => new AssistantProposal(Guid.NewGuid(), ProposalKind.CompleteTask, matches[0].Title, TaskId: matches[0].Id),
            0 => throw new ToolException($"No open task matches '{words}'. Open tasks include: {string.Join("; ", open.Take(10).Select(t => t.Title))}."),
            _ => throw new ToolException($"Several open tasks match '{words}': {string.Join("; ", matches.Take(10).Select(t => t.Title))}. Ask the user which one."),
        };
    }

    // ---------- Helpers ----------

    private static string Required(JsonObject args, string key, int max)
    {
        var value = Str(args, key) ?? throw new ToolException($"'{key}' is required.");
        return value.Length <= max ? value : value[..max];
    }

    private static string? Str(JsonObject args, string key) =>
        args[key] is JsonValue v && v.TryGetValue<string>(out var s) && !string.IsNullOrWhiteSpace(s) ? s.Trim() : null;

    private static DateTime? Date(JsonObject args, string key) =>
        Str(args, key) is { } text
            ? DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : throw new ToolException($"'{key}' must be a date like 2026-10-21.")
            : null;

    private static string? Match(string? value, IEnumerable<string> known) =>
        value is null ? null : known.FirstOrDefault(k => string.Equals(k, value, StringComparison.OrdinalIgnoreCase));

    private static string? Tag(string? value)
    {
        var tag = value?.Trim().TrimStart('#').ToLowerInvariant();
        return string.IsNullOrEmpty(tag) || tag.Contains(' ') ? null : tag;
    }
}
