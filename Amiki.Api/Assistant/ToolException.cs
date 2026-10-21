namespace Amiki.Api.Assistant;

/// <summary>A tool couldn't run with the arguments given; the message goes back to the model so it can retry.</summary>
public sealed class ToolException(string message) : Exception(message);
