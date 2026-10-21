using System.Text.Json.Nodes;

namespace Amiki.Api.Assistant;

/// <summary>
/// The AI model Amiki talks to. Kept to one small method so the provider can be swapped (Gemini
/// today; Claude, OpenAI or a local model later) without touching anything that uses it.
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Asks the model and returns its answer as JSON matching <paramref name="schema"/> (an
    /// OpenAPI-style schema: type, properties, items, enum, nullable, required).
    /// </summary>
    /// <exception cref="LanguageModelException">The model couldn't answer (no key, rate limit, outage…).</exception>
    Task<string> AskForJsonAsync(string instructions, string input, JsonObject schema, CancellationToken ct);
}
