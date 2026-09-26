using Amiki.Core;

namespace Amiki.Modules.Tasks;

public enum Priority { None, Low, Medium, High }

public sealed class TaskItem : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public string? Project { get; set; }
    public DateTime? Due { get; set; }
    public Priority Priority { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    public bool IsDone => CompletedAt is not null;
    public bool IsOverdue => !IsDone && Due?.Date < DateTime.Today;
    public bool IsDueToday => Due?.Date == DateTime.Today;
    /// <summary>Lists keep these showing (struck through) so ticking a task off doesn't make it vanish.</summary>
    public bool CompletedToday => CompletedAt?.Date == DateTime.Today;

    /// <summary>Edit dialogs work on a copy so Cancel leaves the stored task untouched.</summary>
    public TaskItem Clone() => (TaskItem)MemberwiseClone();

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Title) ? "A task needs a title."
        : Title.Length > 500 ? "Title is too long (500 characters max)."
        : Notes?.Length > 5000 ? "Notes are too long (5000 characters max)."
        : null;
}
