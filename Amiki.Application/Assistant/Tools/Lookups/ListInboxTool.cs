using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Domain.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class ListInboxTool(IAmikiDb db) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("list_inbox",
        "Notes the user jotted down quickly and hasn't sorted yet.",
        AssistantArea.Inbox);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var items = await db.Inbox.AsNoTracking().OrderByDescending(i => i.CapturedAt).Take(ToolResult.MaxRows + 1).ToListAsync(ct);
        return ToolResult.Rows(items.Select(i => new JsonObject { ["text"] = i.Text, ["captured"] = ToolResult.Day(i.CapturedAt) }));
    }
}
