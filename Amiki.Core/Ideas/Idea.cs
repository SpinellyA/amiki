using Amiki.Core;
using Amiki.Modules.Tasks;

namespace Amiki.Modules.Ideas;

public sealed class Idea : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Text { get; set; } = "";
    public List<string> Tags { get; set; } = [];
    public IdeaStatus Status { get; set; } = IdeaStatus.Parked;
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public Idea Clone() => new() { Id = Id, Text = Text, Tags = [.. Tags], Status = Status, CreatedAt = CreatedAt };

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
