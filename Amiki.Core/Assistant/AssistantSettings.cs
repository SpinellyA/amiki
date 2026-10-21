using Amiki.Core;

namespace Amiki.Modules.Assistant;

/// <summary>
/// What Amiki may look at and what she knows about you. One set, shared by every device.
/// There's deliberately no switch for the diary: she can never see it.
/// </summary>
public sealed class AssistantSettings : IEntity
{
    /// <summary>There's only ever one; this is its id.</summary>
    public static readonly Guid SingleId = Guid.Parse("a5515700-0000-4000-8000-000000000001");

    public const int MaxAboutMe = 2000;
    public const int MaxPersonality = 500;

    public Guid Id { get; init; } = SingleId;

    /// <summary>A few lines she always knows: your name, school, what you're working on, how you like answers.</summary>
    public string AboutMe { get; set; } = "";

    /// <summary>How she talks. Empty means friendly, short and to the point.</summary>
    public string Personality { get; set; } = "";

    public bool SeeTasks { get; set; } = true;
    public bool SeeIdeas { get; set; } = true;
    public bool SeeMoney { get; set; } = true;
    public bool SeeInbox { get; set; } = true;
    public bool SeeWeeklyNotes { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public AssistantSettings Clone() => (AssistantSettings)MemberwiseClone();

    public string? Validate() =>
        Id != SingleId ? "There's only one set of assistant settings."
        : AboutMe.Length > MaxAboutMe ? $"\"About me\" is too long ({MaxAboutMe} characters max)."
        : Personality.Length > MaxPersonality ? $"Personality is too long ({MaxPersonality} characters max)."
        : null;
}
