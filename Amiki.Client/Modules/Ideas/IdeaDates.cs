using Amiki.Components;

namespace Amiki.Modules.Ideas;

/// <summary>The date an idea shows: when it was archived, finished, or captured, whichever applies.</summary>
public static class IdeaDates
{
    public static string Label(Idea idea) =>
        idea.ArchivedAt is { } archived ? $"archived {archived:MMM d}"
        : idea.Status == IdeaStatus.Done && idea.DoneAt is { } done ? $"archives {done + Idea.ArchiveDoneAfter:MMM d}"
        : When.Short(idea.CreatedAt);
}
