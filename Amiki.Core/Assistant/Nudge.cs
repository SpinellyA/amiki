namespace Amiki.Modules.Assistant;

/// <summary>
/// A small heads-up on the dashboard, worked out by plain rules from your data (no AI), so it's
/// always current: "3 tasks overdue", "Landbank not checked in 16 days"…
/// </summary>
/// <param name="Kind">What it's about (overdue, due-today, review, balance, spending, ideas, inbox); picks the icon.</param>
/// <param name="Href">Where tapping it goes, e.g. "tasks" or "money".</param>
public sealed record Nudge(string Kind, string Text, string Href);
