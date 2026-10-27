namespace Amiki.Domain.People;

/// <summary>
/// Someone allowed to sign in. The owner (you) is created from the config at startup; everyone
/// else is added by the owner on the People page. Each person's data is kept apart by user id.
/// </summary>
public sealed class AppUser
{
    /// <summary>The owner's id. Fixed, so data from before Amiki had people could be handed to you.</summary>
    public static readonly Guid OwnerId = Guid.Parse("00000000-0000-4000-8000-0000000000a1");

    public const int MaxEmail = 320;
    public const int MaxName = 200;

    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Their Google account's address, lowercase.</summary>
    public string Email { get; private set; } = "";

    /// <summary>Their name from Google, filled in when they sign in.</summary>
    public string Name { get; private set; } = "";

    /// <summary>Off means they can't sign in (their data is kept).</summary>
    public bool Enabled { get; private set; } = true;

    /// <summary>Their Gemini key, encrypted (never stored or sent back in the clear).</summary>
    public string? GeminiKeyProtected { get; private set; }

    /// <summary>Who saved the key; empty when there isn't one.</summary>
    public KeySource? GeminiKeySetBy { get; private set; }

    public DateTime AddedAt { get; init; }
    public DateTime? LastSignInAt { get; private set; }

    /// <summary>When they saw the "the owner can view your data, except your diary" notice.</summary>
    public DateTime? NoticeSeenAt { get; private set; }

    public bool IsOwner => Id == OwnerId;

    // For EF Core.
    private AppUser() { }

    /// <summary>Someone the owner lets in.</summary>
    public static AppUser Invite(string email, DateTime now) => new() { Email = Normalize(email), AddedAt = now };

    /// <summary>The owner, who never needs the "what the owner can see" notice.</summary>
    public static AppUser CreateOwner(string email, DateTime now) =>
        new() { Id = OwnerId, Email = Normalize(email), AddedAt = now, NoticeSeenAt = now };

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>The configured owner address may change; the owner can never be locked out.</summary>
    public void BecomeOwnerAs(string email)
    {
        if (!IsOwner) throw new InvalidOperationException("Only the owner's account can be the owner.");
        Email = Normalize(email);
        Enabled = true;
    }

    public void SignedIn(string? name, DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(name)) Name = name.Length > MaxName ? name[..MaxName] : name;
        LastSignInAt = now;
    }

    public void SetEnabled(bool enabled)
    {
        if (IsOwner && !enabled) throw new InvalidOperationException("The owner can't be turned off.");
        Enabled = enabled;
    }

    /// <summary>Saves an (already encrypted) key, or removes it when <paramref name="protectedKey"/> is null.</summary>
    public void SetGeminiKey(string? protectedKey, KeySource by)
    {
        GeminiKeyProtected = protectedKey;
        GeminiKeySetBy = protectedKey is null ? null : by;
    }

    public void SawNotice(DateTime now) => NoticeSeenAt ??= now;
}
