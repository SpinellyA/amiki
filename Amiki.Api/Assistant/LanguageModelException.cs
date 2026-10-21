namespace Amiki.Api.Assistant;

/// <summary>The model couldn't answer. <see cref="Message"/> is safe to show to you.</summary>
/// <param name="RateLimited">The free tier's limit was hit; trying again later will work.</param>
public sealed class LanguageModelException(string message, bool rateLimited = false) : Exception(message)
{
    public bool RateLimited { get; } = rateLimited;
}
