using Amiki.Application.Data;
using Amiki.Domain.People;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.People;

/// <summary>Who may come in: the owner and the people they've added, while their sign-in is on.</summary>
public sealed class SignInService(IAmikiDb db, TimeProvider time)
{
    /// <summary>The allowed person with that address, or null.</summary>
    public Task<AppUser?> FindAllowedAsync(string? email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<AppUser?>(null);
        var normalized = AppUser.Normalize(email);
        return db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == normalized && u.Enabled, ct);
    }

    public Task<AppUser?> FindAllowedAsync(Guid id, CancellationToken ct) =>
        db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id && u.Enabled, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => db.Users.AnyAsync(u => u.Id == id, ct);

    /// <summary>
    /// A Google sign-in finishing. False turns the account away. An address Google itself marks as
    /// unverified is refused (a Gmail address always is verified).
    /// </summary>
    public async Task<bool> SignInAsync(string? email, bool emailVerified, string? name, CancellationToken ct)
    {
        if (!emailVerified || string.IsNullOrWhiteSpace(email)) return false;
        var normalized = AppUser.Normalize(email);
        var person = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized && u.Enabled, ct);
        if (person is null) return false;
        person.SignedIn(name, time.GetLocalNow().DateTime);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
