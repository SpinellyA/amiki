using Amiki.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Users;

/// <summary>Makes sure the owner (from the config) exists, with their starter accounts and categories.</summary>
public static class OwnerAccount
{
    public static async Task EnsureAsync(AmikiDb db, string email, CancellationToken ct = default)
    {
        email = email.Trim().ToLowerInvariant();
        var owner = await db.Users.FirstOrDefaultAsync(u => u.Id == AppUser.OwnerId, ct);
        if (owner is null)
        {
            db.Users.Add(new AppUser { Id = AppUser.OwnerId, Email = email, NoticeSeenAt = DateTime.Now });
        }
        else
        {
            owner.Email = email; // in case the configured owner address changed
            owner.Enabled = true; // the owner can never lock themselves out
        }
        await db.SaveChangesAsync(ct);

        await StarterData.ProvideAsync(db, AppUser.OwnerId, ct);
    }
}
