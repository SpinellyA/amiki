using Amiki.Domain.Assistant;

namespace Amiki.Contracts.People;

/// <summary>What Amiki knows about someone and how she talks to them: set by the owner only.</summary>
public sealed record AssistantWords(string AboutMe, string Personality)
{
    public string? Validate() =>
        AboutMe.Length > AssistantSettings.MaxAboutMe ? $"\"About them\" is too long ({AssistantSettings.MaxAboutMe} characters max)."
        : Personality.Length > AssistantSettings.MaxPersonality ? $"Personality is too long ({AssistantSettings.MaxPersonality} characters max)."
        : null;
}
