using Amiki.Contracts.Assistant;
using Amiki.Application.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;
using Amiki.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant;

/// <summary>
/// Suggests what each Inbox note is (task, idea, expense, income) so you can sort it with one tap.
///
/// What the model is told: the notes, your date, your account / category / project names, and a
/// few recent transactions as examples. What it's never told: your diary, balances, or anything
/// else. Its answer is checked against your real data (<see cref="TriageRules"/>) before use.
/// </summary>
public sealed class InboxTriageService(IAmikiDb db, ILanguageModel model)
{
    private const int ExampleCount = 25;

    public async Task<IReadOnlyList<TriageSuggestion>> SuggestAsync(TriageRequest request, CancellationToken ct)
    {
        var context = await ContextAsync(request.Today, ct);
        var json = await model.AskForJsonAsync(Instructions(context), Input(request.Notes), Schema, ct);
        return Read(json, request.Notes, context);
    }

    // Only names and a few transaction notes. Deliberately no diary, balances, or ideas' text.
    private async Task<TriageContext> ContextAsync(DateTime today, CancellationToken ct)
    {
        var categories = await db.Categories.AsNoTracking()
            .Where(c => c.Name != Category.BalanceFix && c.Name != Category.Fees)
            .Select(c => new { c.Name, c.Kind }).ToListAsync(ct);
        var examples = await db.Transactions.AsNoTracking()
            .Where(t => t.Category != Category.BalanceFix && t.Note != "")
            .OrderByDescending(t => t.CreatedAt).Take(ExampleCount * 3)
            .Select(t => new { t.Note, t.Kind, t.Category, t.Account }).ToListAsync(ct);

        return new TriageContext(
            Today: today.Date,
            Accounts: await db.Accounts.AsNoTracking().OrderBy(a => a.Name).Select(a => a.Name).ToListAsync(ct),
            ExpenseCategories: categories.Where(c => c.Kind == TxKind.Expense).Select(c => c.Name).Order().ToList(),
            IncomeCategories: categories.Where(c => c.Kind == TxKind.Income).Select(c => c.Name).Order().ToList(),
            Projects: (await db.Tasks.AsNoTracking().Where(t => t.Project != null).Select(t => t.Project!).Distinct().ToListAsync(ct)).Order().ToList(),
            Examples: examples
                .DistinctBy(e => e.Note.ToLowerInvariant())
                .Take(ExampleCount)
                .Select(e => new TriageExample(e.Note, e.Kind == TxKind.Income ? "income" : "expense", e.Category, e.Account))
                .ToList());
    }

    private static string Instructions(TriageContext c)
    {
        var s = new StringBuilder();
        s.AppendLine("You sort quick notes a student jotted on their phone into their personal app. Amounts are in Philippine pesos.");
        s.AppendLine("For every note, return one item with the same id and decide its kind:");
        s.AppendLine("- task: something to do (homework, errands, deadlines). Set due (yyyy-MM-dd) only if a day or date is mentioned, resolving words like 'tomorrow' or 'thu' from today's date. Set project from a #tag or a matching project. Set priority only if the note signals urgency.");
        s.AppendLine("- idea: a thought, plan or 'what if' to keep, not a to-do.");
        s.AppendLine("- expense: money spent (needs an amount). income: money received (needs an amount). Pick the category and account from the lists, guided by the examples; use 'Cash' if no account is clear.");
        s.AppendLine("- unsure: anything else, or if you can't tell.");
        s.AppendLine("title: a short clean version of the note without #tags, @days or the amount. Keep the note's language. reason: under 8 words.");
        s.AppendLine();
        s.AppendLine($"Today: {c.Today:yyyy-MM-dd} ({c.Today:dddd}).");
        s.AppendLine($"Accounts: {string.Join(", ", c.Accounts)}.");
        s.AppendLine($"Expense categories: {string.Join(", ", c.ExpenseCategories)}.");
        s.AppendLine($"Income categories: {string.Join(", ", c.IncomeCategories)}.");
        s.AppendLine($"Projects: {(c.Projects.Count == 0 ? "none yet" : string.Join(", ", c.Projects))}.");
        if (c.Examples.Count > 0)
        {
            s.AppendLine("Recent transactions (note → kind, category, account):");
            foreach (var e in c.Examples) s.AppendLine($"- \"{e.Note}\" → {e.Kind}, {e.Category}, {e.Account}");
        }
        return s.ToString();
    }

    private static string Input(IEnumerable<TriageNote> notes) =>
        JsonSerializer.Serialize(notes.Select(n => new { id = n.Id, text = n.Text }));

    private static readonly JsonObject Schema = (JsonObject)JsonNode.Parse("""
        {
          "type": "array",
          "items": {
            "type": "object",
            "properties": {
              "id": { "type": "string" },
              "kind": { "type": "string", "enum": ["task", "idea", "expense", "income", "unsure"] },
              "title": { "type": "string" },
              "due": { "type": "string", "nullable": true, "description": "yyyy-MM-dd" },
              "project": { "type": "string", "nullable": true },
              "priority": { "type": "string", "nullable": true, "enum": ["low", "medium", "high"] },
              "amount": { "type": "number", "nullable": true },
              "category": { "type": "string", "nullable": true },
              "account": { "type": "string", "nullable": true },
              "reason": { "type": "string" }
            },
            "required": ["id", "kind", "title"]
          }
        }
        """)!;

    private sealed record Answer(string? Id, string? Kind, string? Title, string? Due, string? Project, string? Priority,
        decimal? Amount, string? Category, string? Account, string? Reason);

    /// <summary>Turns the model's JSON into checked suggestions, one per note it answered for. Unknown ids are ignored.</summary>
    internal static IReadOnlyList<TriageSuggestion> Read(string json, IReadOnlyList<TriageNote> notes, TriageContext context)
    {
        List<Answer>? answers;
        try { answers = JsonSerializer.Deserialize<List<Answer>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { throw new LanguageModelException("Gemini's answer wasn't in the expected shape. Try again."); }

        var byId = notes.ToDictionary(n => n.Id);
        var suggestions = new List<TriageSuggestion>();
        foreach (var a in answers ?? [])
        {
            if (!Guid.TryParse(a.Id, out var id) || !byId.TryGetValue(id, out var note) || suggestions.Any(s => s.NoteId == id)) continue;
            var raw = new TriageSuggestion(
                NoteId: id,
                Kind: Enum.TryParse<TriageKind>(a.Kind, ignoreCase: true, out var kind) ? kind : TriageKind.Unsure,
                Title: a.Title ?? "",
                Due: DateTime.TryParseExact(a.Due, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var due) ? due : null,
                Project: a.Project,
                Priority: Enum.TryParse<Priority>(a.Priority, ignoreCase: true, out var priority) ? priority : Priority.None,
                Amount: a.Amount,
                Category: a.Category,
                Account: a.Account,
                Reason: a.Reason);
            suggestions.Add(TriageRules.Clean(raw, note, context));
        }
        return suggestions;
    }
}
