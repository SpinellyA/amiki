using Amiki.Domain;
using Amiki.Domain.Tasks;

namespace Amiki.Domain.Ideas;

public sealed class Idea : IEntity
{
    /// <summary>Done ideas leave the board this long after they're finished, into the archive.</summary>
    public static readonly TimeSpan ArchiveDoneAfter = TimeSpan.FromDays(14);

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public IdeaStatus Status { get; set; } = IdeaStatus.Parked;
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    /// <summary>When it last became Done; empty while it isn't.</summary>
    public DateTime? DoneAt { get; set; }
    /// <summary>When it went into the archive; empty while it's on the board.</summary>
    public DateTime? ArchivedAt { get; set; }

    public bool IsArchived => ArchivedAt is not null;

    public Idea Clone() => new()
    {
        Id = Id, Text = Text, Tags = [.. Tags], Status = Status, CreatedAt = CreatedAt, DoneAt = DoneAt, ArchivedAt = ArchivedAt,
    };

    /// <summary>
    /// A copy moved to <paramref name="status"/>. Becoming Done starts the archive countdown; moving
    /// anywhere else brings an archived idea back, since it's active again.
    /// </summary>
    public Idea WithStatus(IdeaStatus status, DateTime now)
    {
        var moved = Clone();
        if (moved.Status != status) moved.DoneAt = status == IdeaStatus.Done ? now : null;
        moved.Status = status;
        if (status != IdeaStatus.Done) moved.ArchivedAt = null;
        return moved;
    }

    public Idea Archived(DateTime now)
    {
        var archived = Clone();
        archived.ArchivedAt ??= now;
        return archived;
    }

    /// <summary>Back on the board. A done idea gets a fresh countdown, so it doesn't vanish again at once.</summary>
    public Idea Restored(DateTime now)
    {
        var restored = Clone();
        restored.ArchivedAt = null;
        if (restored.Status == IdeaStatus.Done) restored.DoneAt = now;
        return restored;
    }

    public bool ShouldAutoArchive(DateTime now) =>
        !IsArchived && Status == IdeaStatus.Done && DoneAt is { } done && now - done >= ArchiveDoneAfter;

    /// <summary>
    /// Where an idea stands given the tasks made from it: in progress while any is open, done once
    /// all are. With no tasks, it stays wherever it is (you manage it by hand).
    /// </summary>
    public static IdeaStatus? StatusFromTasks(IEnumerable<TaskItem> linkedTasks)
    {
        var tasks = linkedTasks.ToList();
        if (tasks.Count == 0) return null;
        return tasks.All(t => t.IsDone) ? IdeaStatus.Done : IdeaStatus.InProgress;
    }

    /// <summary>"Auto study planner from exam dates #amiki #school" → text + tags.</summary>
    public static Idea Parse(string input, DateTime? createdAt = null)
    {
        var tokens = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return new Idea
        {
            Text = string.Join(' ', tokens.Where(t => !IsTag(t))),
            Tags = tokens.Where(IsTag).Select(t => t[1..].ToLowerInvariant()).Distinct().ToList(),
            CreatedAt = createdAt ?? DateTime.Now,
        };
    }

    /// <summary>Round-trips through Parse, so editing is just editing text.</summary>
    public string ToEditableText() => string.Join(' ', Tags.Select(t => "#" + t).Prepend(Text));

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Text) ? "An idea can't be empty."
        : Text.Length > 5000 ? "Idea is too long (5000 characters max)."
        : null;

    private static bool IsTag(string token) => token.Length > 1 && token[0] == '#';
}
