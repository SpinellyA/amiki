namespace Amiki.Contracts.Assistant;

/// <param name="Now">Your local time, so "today" is your today.</param>
/// <param name="Rewrite">Ask her to write today's note again instead of showing the saved one.</param>
public sealed record BriefingRequest(DateTime Now, bool Rewrite = false);
