using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Domain.Assistant;
using Amiki.Domain.Clock;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class DayScheduleTool(IAmikiDb db) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("day_schedule",
        "The time blocks the user planned for a day on their Clock: what each block is for and when it starts and ends. Use for 'what's on my schedule', 'what should I be doing now', 'when am I free'.",
        AssistantArea.Tasks,
        """{"type":"object","properties":{"date":{"type":"string","description":"yyyy-MM-dd; omit for today"}}}""");

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var day = args.Date("date") ?? today;
        var plan = new DayPlan(day, await db.TimeBlocks.AsNoTracking().Where(b => b.Day == day).ToListAsync(ct));
        var result = ToolResult.Rows(plan.Blocks.Select(b => new JsonObject
        {
            ["from"] = b.Starts.ToString("h:mm tt"),
            ["to"] = b.Ends.ToString("h:mm tt"),
            ["for"] = b.Label,
            ["is_a_task"] = b.TaskId is not null,
        }));
        result["day"] = ToolResult.Day(day);
        result["planned_hours"] = Math.Round(plan.PlannedMinutes / 60.0, 2);
        return result;
    }
}
