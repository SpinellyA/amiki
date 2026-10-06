namespace Amiki.Modules.Ideas;

/// <summary>Where an idea stands. Fixed on purpose: three columns, no setup.</summary>
public enum IdeaStatus
{
    /// <summary>Captured and set aside for later. Every new idea starts here.</summary>
    Parked,
    /// <summary>Being worked on: you made a task from it, or moved it here yourself.</summary>
    InProgress,
    /// <summary>Finished: its tasks are all done, or you said so.</summary>
    Done,
}
