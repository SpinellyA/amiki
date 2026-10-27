using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Domain.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class WeeklyNotesTool(IAmikiDb db) : ILookupTool
{
    private const int DefaultWeeks = 4;
    private const int MaxWeeks = 12;

    public ToolDefinition Definition { get; } = ToolDefinition.Create("weekly_notes",
        "The user's weekly reflections: what went well and what to change, newest week first.",
        AssistantArea.WeeklyNotes,
        """{"type":"object","properties":{"weeks":{"type":"integer","description":"How many recent weeks, 1-12"}}}""");

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var weeks = Math.Clamp(args.Number("weeks") ?? DefaultWeeks, 1, MaxWeeks);
        var notes = await db.WeeklyNotes.AsNoTracking().OrderByDescending(n => n.WeekStart).Take(weeks).ToListAsync(ct);
        return ToolResult.Rows(notes.Select(n => new JsonObject
        {
            ["week_of"] = ToolResult.Day(n.WeekStart),
            ["went_well"] = n.WentWell,
            ["to_change"] = n.ToChange,
        }));
    }
}
