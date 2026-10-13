namespace Amiki.Modules.Ideas;

/// <summary>Ways to lay out the Ideas page. Each device remembers the one you picked.</summary>
public enum IdeaLayout
{
    /// <summary>Three columns, Parked | In progress | Done; drag between them.</summary>
    Board,
    /// <summary>Compact one-line rows, grouped by status: the most ideas on screen at once.</summary>
    List,
    /// <summary>A grid of cards, in-progress first, each labelled with its status.</summary>
    Cards,
}
