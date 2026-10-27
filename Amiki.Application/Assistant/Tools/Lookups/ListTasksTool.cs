using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Domain.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class ListTasksTool(IAmikiDb db) : ILookupTool
{
    private const int RecentDays = 14;

    public ToolDefinition Definition { get; } = ToolDefinition.Create("list_tasks",
        "Lists the user's tasks. Use for questions about to-dos, deadlines, what's due, overdue or recently finished.",
        AssistantArea.Tasks, """
        {"type":"object","properties":{
          "view":{"type":"string","enum":["due_today","overdue","upcoming","open","done_recently"],"description":"due_today, overdue, upcoming (due after today), open (all unfinished), done_recently (finished in the last 14 days)"},
          "project":{"type":"string","description":"Only tasks with this project tag, without #"}},
         "required":["view"]}
        """);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var project = args.Text("project")?.TrimStart('#').ToLowerInvariant();
        var tomorrow = today.AddDays(1);
        var recent = today.AddDays(-RecentDays);

        var query = db.Tasks.AsNoTracking();
        if (project is not null) query = query.Where(t => t.Project == project);
        query = args.Text("view") switch
        {
            "due_today" => query.Where(t => t.CompletedAt == null && t.Due >= today && t.Due < tomorrow),
            "overdue" => query.Where(t => t.CompletedAt == null && t.Due < today),
            "upcoming" => query.Where(t => t.CompletedAt == null && t.Due >= tomorrow),
            "done_recently" => query.Where(t => t.CompletedAt >= recent),
            _ => query.Where(t => t.CompletedAt == null),
        };
        var tasks = await query.OrderBy(t => t.Due == null).ThenBy(t => t.Due).Take(ToolResult.MaxRows + 1).ToListAsync(ct);
        return ToolResult.Rows(tasks.Select(t => new JsonObject
        {
            ["title"] = t.Title,
            ["due"] = ToolResult.Day(t.Due),
            ["project"] = t.Project,
            ["priority"] = t.Priority.ToString().ToLowerInvariant(),
            ["done"] = ToolResult.Day(t.CompletedAt),
            ["from_an_idea"] = t.IdeaId is not null,
        }));
    }
}
