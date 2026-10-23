namespace Amiki.Api.Users;

/// <summary>
/// Someone allowed to sign in. The owner (you) is created from the config at startup; everyone
/// else is added by the owner on the People page. Each person's data is kept apart by user id.
/// </summary>
public sealed class AppUser
{
    /// <summary>The owner's id. Fixed, so data from before Amiki had users could be handed to you.</summary>
    public static readonly Guid OwnerId = Guid.Parse("00000000-0000-4000-8000-0000000000a1");

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Their Google account's address, lowercase.</summary>
    public string Email { get; set; } = "";

    /// <summary>Their name from Google, filled in when they first sign in.</summary>
    public string Name { get; set; } = "";

    /// <summary>Off means they can't sign in (their data is kept).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Their Gemini key, encrypted (never stored or sent back in the clear).</summary>
    public string? GeminiKeyProtected { get; set; }

    /// <summary>Who set the key: "owner" or "self".</summary>
    public string? GeminiKeySetBy { get; set; }

    public DateTime AddedAt { get; init; } = DateTime.Now; // wall-clock, like every date in Amiki
    public DateTime? LastSignInAt { get; set; }

    /// <summary>When they saw the "the owner can view your data, except your diary" notice.</summary>
    public DateTime? NoticeSeenAt { get; set; }

    public bool IsOwner => Id == OwnerId;
}
