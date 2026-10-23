using Amiki.Api.Data;
using Amiki.Modules.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Users;

/// <summary>
/// What every new person starts with: the four accounts (at zero, like before) and the starter
/// categories. Safe to run again: it only fills in what that person doesn't have yet.
/// </summary>
public static class StarterData
{
    private static readonly string[] Accounts = ["Cash", "GCash", "Maribank", "Landbank"];

    public static async Task ProvideAsync(AmikiDb db, Guid userId, CancellationToken ct = default)
    {
        if (!await Owned(db.Accounts, userId).AnyAsync(ct))
            foreach (var name in Accounts) AddFor(db, new Account(name, 0), userId);

        if (!await Owned(db.Categories, userId).AnyAsync(ct))
            foreach (var category in StarterCategories.Create()) AddFor(db, category, userId);

        await db.SaveChangesAsync(ct);
    }

    /// <summary>A person's rows regardless of who the request is for (admin work only).</summary>
    public static IQueryable<T> Owned<T>(IQueryable<T> set, Guid userId) where T : class =>
        set.IgnoreQueryFilters().Where(e => EF.Property<Guid>(e, AmikiDb.UserIdColumn) == userId);

    /// <summary>Adds a row that belongs to that person, whoever the request is for (admin work only).</summary>
    public static void AddFor<T>(AmikiDb db, T entity, Guid userId) where T : class =>
        db.Add(entity).Property(AmikiDb.UserIdColumn).CurrentValue = userId;
}
