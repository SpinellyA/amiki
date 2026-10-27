using System.Text.Json.Nodes;

namespace Amiki.Application.Assistant;

/// <summary>Something Amiki can look up, described so the model knows when and how to ask for it.</summary>
/// <param name="Parameters">OpenAPI-style object schema for the arguments, or null if it takes none.</param>
/// <param name="Area">What it reads, shown under her reply ("tasks", "money"…).</param>
public sealed record ToolDefinition(string Name, string Description, JsonObject? Parameters, string Area);
