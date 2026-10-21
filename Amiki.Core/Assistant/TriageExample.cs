namespace Amiki.Modules.Assistant;

/// <summary>A past transaction the model can learn your habits from ("Gas" → Transport, Cash).</summary>
public sealed record TriageExample(string Note, string Kind, string Category, string Account);
