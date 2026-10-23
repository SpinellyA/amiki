using Microsoft.AspNetCore.DataProtection;

namespace Amiki.Api.Users;

/// <summary>
/// Encrypts the API keys people save, using the same data-protection keys as the sign-in
/// cookie (kept in the database). A stolen database dump alone doesn't reveal anyone's key.
/// </summary>
public sealed class ApiKeyProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector = provider.CreateProtector("Amiki.ApiKeys");

    public string Protect(string key) => _protector.Protect(key.Trim());

    /// <summary>The key, or null if there isn't one (or it can no longer be read).</summary>
    public string? Unprotect(string? protectedKey)
    {
        if (string.IsNullOrEmpty(protectedKey)) return null;
        try { return _protector.Unprotect(protectedKey); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
}
