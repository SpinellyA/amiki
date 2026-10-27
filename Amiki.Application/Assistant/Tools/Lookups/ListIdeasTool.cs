using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Domain.Assistant;
using Amiki.Domain.Ideas;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class ListIdeasTool(IAmikiDb db) : ILookupTool
{
    private const int MaxText = 300;

    public ToolDefinition Definition { get; } = ToolDefinition.Create("list_ideas",
        "Lists the user's ideas with their status (parked, in progress, done).",
        AssistantArea.Ideas, """
        {"type":"object","properties":{
          "status":{"type":"string","enum":["parked","in_progress","done","any"]},
          "search":{"type":"string","description":"Only ideas containing this text or tag"},
          "include_archived":{"type":"boolean"}}}
        """);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        IdeaStatus? status = args.Text("status") switch
        {
            "parked" => IdeaStatus.Parked,
            "in_progress" => IdeaStatus.InProgress,
            "done" => IdeaStatus.Done,
            _ => null,
        };
        var search = args.Text("search");
        var archived = args.Flag("include_archived");

        var ideas = await db.Ideas.AsNoTracking().OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        return ToolResult.Rows(ideas
            .Where(i => archived || !i.IsArchived)
            .Where(i => status is null || i.Status == status)
            .Where(i => search is null || i.ToEditableText().Contains(search, StringComparison.OrdinalIgnoreCase))
            .Select(i => new JsonObject
            {
                ["text"] = i.Text.Length > MaxText ? i.Text[..MaxText] + "…" : i.Text,
                ["tags"] = new JsonArray(i.Tags.Select(t => (JsonNode)JsonValue.Create(t)!).ToArray()),
                ["status"] = i.Status == IdeaStatus.InProgress ? "in progress" : i.Status.ToString().ToLowerInvariant(),
                ["captured"] = ToolResult.Day(i.CreatedAt),
                ["archived"] = i.IsArchived,
            }));
    }
}
