namespace Amiki.Domain.People;

/// <summary>Who saved someone's Gemini key.</summary>
public enum KeySource
{
    /// <summary>The owner added it for them.</summary>
    Owner,
    /// <summary>They added their own.</summary>
    Self,
}
