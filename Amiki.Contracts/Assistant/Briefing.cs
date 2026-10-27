namespace Amiki.Contracts.Assistant;

/// <summary>What the dashboard shows from Amiki today.</summary>
/// <param name="Text">Her short note for today, or empty if she's off or couldn't write it.</param>
/// <param name="WrittenAt">When the note was written (once a day, shared by your devices).</param>
/// <param name="Nudges">Current heads-ups, always fresh.</param>
/// <param name="Problem">Why there's no note, if something went wrong (e.g. Gemini was busy).</param>
public sealed record Briefing(string? Text, DateTime? WrittenAt, IReadOnlyList<Nudge> Nudges, string? Problem = null);
