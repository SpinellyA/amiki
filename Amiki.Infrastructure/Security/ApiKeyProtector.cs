using Amiki.Application.People;
using Microsoft.AspNetCore.DataProtection;

namespace Amiki.Infrastructure.Security;

/// <summary>
/// Encrypts the API keys people save, using the same data-protection keys as the sign-in
/// cookie (kept in the database). A stolen database dump alone doesn't reveal anyone's key.
/// </summary>
public sealed class ApiKeyProtector(IDataProtectionProvider provider) : ISecretProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Amiki.ApiKeys");

    public string Protect(string secret) => _protector.Protect(secret.Trim());

    public string? Unprotect(string? protectedSecret)
    {
        if (string.IsNullOrEmpty(protectedSecret)) return null;
        try { return _protector.Unprotect(protectedSecret); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
}
