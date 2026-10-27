namespace Amiki.Contracts.People;

/// <summary>Save a Gemini API key; empty removes it.</summary>
public sealed record GeminiKeyRequest(string? Key)
{
    public const int MaxLength = 200;

    public string? Validate() => Problem(Key);

    internal static string? Problem(string? key) =>
        key is not null && (key.Trim().Length > MaxLength || key.Trim().Contains(' '))
            ? "That doesn't look like a Gemini API key."
            : null;
}
