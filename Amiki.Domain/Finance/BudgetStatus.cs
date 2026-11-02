namespace Amiki.Domain.Finance;

/// <summary>How spending is going against a budget.</summary>
public enum BudgetStatus
{
    /// <summary>Hasn't started yet.</summary>
    Upcoming,
    /// <summary>At this rate you'll stay within it.</summary>
    OnTrack,
    /// <summary>Still within it, but at this rate you'll go over by the end.</summary>
    AtRisk,
    /// <summary>Already spent more than the budget.</summary>
    Over,
}
