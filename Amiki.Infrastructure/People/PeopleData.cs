using Amiki.Application.People;
using Amiki.Domain.Assistant;
using Amiki.Domain.Finance;
using Amiki.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Infrastructure.People;

/// <summary>
/// The only place that works past the "this request's person" query filter, always for one
/// named person. Used by the owner's People features and at startup for the owner.
/// </summary>
public sealed class PeopleData(AmikiDb db) : IPeopleData
{
    public async Task ProvideStarterDataAsync(Guid userId, CancellationToken ct)
    {
        if (!await OwnedBy(db.Accounts, userId).AnyAsync(ct))
            foreach (var account in StarterAccounts.Create()) AddFor(account, userId);

        if (!await OwnedBy(db.Categories, userId).AnyAsync(ct))
            foreach (var category in StarterCategories.Create()) AddFor(category, userId);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteEverythingAsync(Guid userId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Every table with a UserId column, so nothing of theirs is left behind (new tables included).
        // Table names come from Amiki's own model, never from a request; the id is a parameter.
        foreach (var table in db.Model.GetEntityTypes().Where(t => t.FindProperty(AmikiDb.UserIdColumn) is not null).Select(t => t.GetTableName()).Distinct())
        {
            var sql = $"DELETE FROM \"{table}\" WHERE \"{AmikiDb.UserIdColumn}\" = {{0}}";
            await db.Database.ExecuteSqlRawAsync(sql, [userId], ct);
        }
        await db.Users.Where(u => u.Id == userId).ExecuteDeleteAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<AssistantSettings> AssistantSettingsForAsync(Guid userId, CancellationToken ct)
    {
        if (await OwnedBy(db.AssistantSettings, userId).FirstOrDefaultAsync(ct) is { } settings) return settings;
        settings = new AssistantSettings();
        AddFor(settings, userId);
        return settings;
    }

    private static IQueryable<T> OwnedBy<T>(IQueryable<T> set, Guid userId) where T : class =>
        set.IgnoreQueryFilters().Where(e => EF.Property<Guid>(e, AmikiDb.UserIdColumn) == userId);

    private void AddFor<T>(T entity, Guid userId) where T : class =>
        db.Add(entity).Property(AmikiDb.UserIdColumn).CurrentValue = userId;
}
