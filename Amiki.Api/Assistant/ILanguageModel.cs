using System.Text.Json.Nodes;
using Amiki.Modules.Assistant;

namespace Amiki.Api.Assistant;

/// <summary>
/// The AI model Amiki talks to. Kept small so the provider can be swapped (Gemini today; Claude,
/// OpenAI or a local model later) without touching anything that uses it.
/// </summary>
public interface ILanguageModel
{
    /// <summary>Name shown in settings, e.g. "Gemini · gemini-2.5-flash".</summary>
    string Description { get; }

    /// <summary>
    /// Asks the model and returns its answer as JSON matching <paramref name="schema"/> (an
    /// OpenAPI-style schema: type, properties, items, enum, nullable, required).
    /// </summary>
    /// <exception cref="LanguageModelException">The model couldn't answer (no key, rate limit, outage…).</exception>
    Task<string> AskForJsonAsync(string instructions, string input, JsonObject schema, CancellationToken ct);

    /// <summary>
    /// One chat turn. The model may call <paramref name="tools"/> (through <paramref name="runTool"/>,
    /// which returns JSON) any number of times, up to a limit, before giving its text reply.
    /// </summary>
    /// <exception cref="LanguageModelException">The model couldn't answer.</exception>
    Task<string> ChatAsync(
        string instructions,
        IReadOnlyList<ChatLine> history,
        string message,
        IReadOnlyList<ToolDefinition> tools,
        Func<ToolCall, CancellationToken, Task<JsonNode>> runTool,
        CancellationToken ct);
}
