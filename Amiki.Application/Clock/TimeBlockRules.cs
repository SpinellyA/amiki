using Amiki.Application.Data;
using Amiki.Application.Entities;
using Amiki.Domain.Clock;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Clock;

/// <summary>Two blocks can't share the same time on a day.</summary>
public sealed class TimeBlockRules(IAmikiDb db) : EntityRules<TimeBlock>
{
    public override async Task<string?> CheckAsync(TimeBlock incoming, CancellationToken ct)
    {
        var sameDay = await db.TimeBlocks.AsNoTracking().Where(b => b.Day == incoming.Day && b.Id != incoming.Id).ToListAsync(ct);
        return sameDay.FirstOrDefault(incoming.Overlaps) is { } clash
            ? $"That overlaps “{clash.Label}” ({Clock(clash.StartMinute)}–{Clock(clash.EndMinute)})."
            : null;
    }

    private static string Clock(int minute) => DateTime.MinValue.AddMinutes(minute % TimeBlock.MinutesInDay).ToString("h:mm tt");
}
