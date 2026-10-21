namespace Amiki.Modules.Assistant;

/// <summary>Whether the server has a model key, and which model answers.</summary>
public sealed record AssistantStatus(bool Enabled, string Model);
