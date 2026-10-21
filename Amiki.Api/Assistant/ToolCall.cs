using System.Text.Json.Nodes;

namespace Amiki.Api.Assistant;

/// <summary>The model asking to use a tool, with the arguments it chose.</summary>
public sealed record ToolCall(string Name, JsonObject Args);
