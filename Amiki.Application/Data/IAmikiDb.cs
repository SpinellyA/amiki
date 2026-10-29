using Amiki.Domain;
using Amiki.Domain.Assistant;
using Amiki.Domain.Diary;
using Amiki.Domain.Finance;
using Amiki.Domain.Ideas;
using Amiki.Domain.Inbox;
using Amiki.Domain.People;
using Amiki.Domain.Review;
using Amiki.Domain.Tasks;
using Amiki.Domain.Wishlist;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amiki.Application.Data;

/// <summary>
/// The database, as the use cases see it. Every personal set only ever holds the rows of the
/// person the request is for (<see cref="People.ICurrentUser.DataUserId"/>), and new rows are
/// given that person on save, so nothing here has to filter by person itself.
/// </summary>
public interface IAmikiDb
{
    /// <summary>Who can sign in. Not per person.</summary>
    DbSet<AppUser> Users { get; }

    DbSet<InboxItem> Inbox { get; }
    DbSet<TaskItem> Tasks { get; }
    DbSet<Idea> Ideas { get; }
    DbSet<Category> Categories { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Transfer> Transfers { get; }
    DbSet<BalanceCheck> BalanceChecks { get; }
    DbSet<AuditEntry> AuditLog { get; }
    DbSet<Plan> Plans { get; }
    DbSet<WeeklyNote> WeeklyNotes { get; }
    DbSet<DiaryEntry> Diary { get; }
    DbSet<AssistantSettings> AssistantSettings { get; }
    DbSet<DailyBriefing> DailyBriefings { get; }
    DbSet<WishItem> Wishlist { get; }
    DbSet<WishlistFund> WishlistFunds { get; }

    DbSet<T> Set<T>() where T : class;

    EntityEntry<T> Entry<T>(T entity) where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
