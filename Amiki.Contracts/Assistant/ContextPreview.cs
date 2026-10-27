namespace Amiki.Contracts.Assistant;

/// <summary>Exactly what Amiki is told before your next message, for the settings page.</summary>
/// <param name="Model">Which model answers, e.g. "Gemini · gemini-2.5-flash".</param>
/// <param name="Instructions">Her full standing instructions, as sent.</param>
/// <param name="Tools">What she can look up: name and description.</param>
public sealed record ContextPreview(string Model, string Instructions, IReadOnlyList<string> Tools);
