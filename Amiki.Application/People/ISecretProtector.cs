namespace Amiki.Application.People;

/// <summary>Encrypts secrets people save (their API keys), so a database dump alone doesn't reveal them.</summary>
public interface ISecretProtector
{
    string Protect(string secret);

    /// <summary>The secret, or null if there isn't one (or it can no longer be read).</summary>
    string? Unprotect(string? protectedSecret);
}
