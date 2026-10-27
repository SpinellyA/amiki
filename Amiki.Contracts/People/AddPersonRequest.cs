namespace Amiki.Contracts.People;

/// <summary>Let a Google account sign in, optionally with a Gemini key for them.</summary>
public sealed record AddPersonRequest(string Email, string? GeminiKey = null)
{
    public const int MaxEmail = 320;

    public string? Validate()
    {
        var email = Email.Trim();
        return email.Length == 0 ? "Type their Google address."
            : email.Length > MaxEmail || email.Count(c => c == '@') != 1 || email.StartsWith('@') || email.EndsWith('@') || email.Contains(' ')
                ? "That doesn't look like an email address."
            : GeminiKeyRequest.Problem(GeminiKey);
    }
}
