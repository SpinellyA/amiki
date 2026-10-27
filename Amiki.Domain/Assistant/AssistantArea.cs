namespace Amiki.Domain.Assistant;

/// <summary>A part of your data Amiki can look at or suggest changes to. (The diary is never one.)</summary>
public enum AssistantArea
{
    Tasks,
    Ideas,
    Money,
    Inbox,
    WeeklyNotes,
    /// <summary>What she knows about you ("remember this").</summary>
    Memory,
}
