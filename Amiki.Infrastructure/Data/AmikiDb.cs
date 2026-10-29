using System.Text.Json;
using Amiki.Application.Data;
using Amiki.Application.People;
using Amiki.Domain;
using Amiki.Domain.People;
using Amiki.Domain.Assistant;
using Amiki.Domain.Diary;
using Amiki.Domain.Finance;
using Amiki.Domain.Ideas;
using Amiki.Domain.Inbox;
using Amiki.Domain.Review;
using Amiki.Domain.Tasks;
using Amiki.Domain.Wishlist;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amiki.Infrastructure.Data;

/// <summary>
/// The database. Every personal table has a hidden UserId column: reads only ever see the rows of
/// the person the request is for (<see cref="ICurrentUser.DataUserId"/>), and new rows are stamped
/// with it on save. That's what keeps one person's data from another's; nothing else has to
/// remember to filter. Keys include the user, so things with fixed ids (a day's diary page, the
/// assistant settings) can exist once per person.
/// </summary>
public sealed class AmikiDb(DbContextOptions<AmikiDb> options, ICurrentUser user) : DbContext(options), IAmikiDb, IDataProtectionKeyContext
{
    public const string UserIdColumn = "UserId";

    /// <summary>Sign-in cookie encryption keys; kept here so restarts don't sign you out.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    /// <summary>Who can sign in. Not per-person itself.</summary>
    public DbSet<AppUser> Users => Set<AppUser>();

    // Read by the query filters each time a query runs, so it always means this request's person.
    private Guid CurrentUserId => user.DataUserId;

    public DbSet<InboxItem> Inbox => Set<InboxItem>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<BalanceCheck> BalanceChecks => Set<BalanceCheck>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<WeeklyNote> WeeklyNotes => Set<WeeklyNote>();
    public DbSet<DiaryEntry> Diary => Set<DiaryEntry>();
    public DbSet<AssistantSettings> AssistantSettings => Set<AssistantSettings>();
    public DbSet<DailyBriefing> DailyBriefings => Set<DailyBriefing>();
    public DbSet<WishItem> Wishlist => Set<WishItem>();
    public DbSet<WishlistFund> WishlistFunds => Set<WishlistFund>();

    private static readonly JsonSerializerOptions Json = AmikiJson.Create();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Dates are wall-clock "your day" values (see WallClockDateTimeConverter), so they're
        // stored without a time zone rather than converted to UTC.
        builder.Properties<DateTime>().HaveColumnType("timestamp without time zone");
        builder.Properties<DateTime?>().HaveColumnType("timestamp without time zone");
        builder.Properties<decimal>().HavePrecision(14, 2);
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<DataProtectionKey>().ToTable("data_protection_keys");

        // Per person: the hidden UserId column, a key that includes it, and the filter. First, so
        // the indexes below can use the column.
        PerPerson<InboxItem>(model);
        PerPerson<TaskItem>(model);
        PerPerson<Idea>(model);
        PerPerson<Category>(model);
        PerPerson<Transaction>(model);
        PerPerson<Account>(model, key: nameof(Account.Name));
        PerPerson<Transfer>(model);
        PerPerson<BalanceCheck>(model);
        PerPerson<AuditEntry>(model, key: null); // its own running number is already unique
        PerPerson<Plan>(model);
        PerPerson<WeeklyNote>(model);
        PerPerson<DiaryEntry>(model);
        PerPerson<AssistantSettings>(model);
        PerPerson<DailyBriefing>(model);
        PerPerson<WishItem>(model);
        PerPerson<WishlistFund>(model);

        model.Entity<InboxItem>().ToTable("inbox_items");

        model.Entity<WishItem>(wish =>
        {
            wish.ToTable("wish_items");
            wish.Property(w => w.Name).HasMaxLength(WishItem.MaxName);
            wish.Property(w => w.Link).HasMaxLength(WishItem.MaxLink);
            wish.Property(w => w.Note).HasMaxLength(WishItem.MaxNote);
            wish.Property(w => w.Status).HasConversion<string>().HasMaxLength(16);
        });
        model.Entity<WishlistFund>().ToTable("wishlist_funds");

        model.Entity<TaskItem>(task =>
        {
            task.ToTable("tasks");
            task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);
            task.HasIndex(t => t.IdeaId);
        });

        // Tags map to a Postgres text[] column.
        model.Entity<Idea>(idea =>
        {
            idea.ToTable("ideas");
            idea.Property(i => i.Status).HasConversion<string>().HasMaxLength(16);
        });

        model.Entity<Transaction>(tx =>
        {
            tx.ToTable("transactions");
            tx.Property(t => t.Kind).HasConversion<string>().HasMaxLength(16);
            tx.HasIndex(t => t.Date);
        });

        model.Entity<Category>(category =>
        {
            category.ToTable("categories");
            category.Property(c => c.Kind).HasConversion<string>().HasMaxLength(16);
            category.Property(c => c.Name).HasMaxLength(Category.MaxNameLength);
            category.Property(c => c.Icon).HasMaxLength(40);
            category.HasIndex(UserIdColumn, nameof(Category.Kind), nameof(Category.Name)).IsUnique();
        });

        // Start at zero: the first balance check on each account sets its real amount (as a
        // labeled correction), instead of made-up opening balances skewing every total.
        // (Each person's starter accounts and categories are made when they're added.)
        model.Entity<Account>(account => account.ToTable("accounts"));

        model.Entity<Transfer>(transfer =>
        {
            transfer.ToTable("transfers");
            transfer.HasIndex(t => t.Date);
        });

        model.Entity<BalanceCheck>(check =>
        {
            check.ToTable("balance_checks");
            check.HasIndex(c => new { c.Account, c.CheckedAt });
        });

        model.Entity<AuditEntry>(entry =>
        {
            entry.ToTable("audit_log");
            entry.Property(e => e.Id).UseIdentityAlwaysColumn();
            entry.Property(e => e.Before).HasColumnType("jsonb");
            entry.Property(e => e.After).HasColumnType("jsonb");
            entry.HasIndex(e => e.At);
            entry.HasIndex(e => e.EntityId);
        });

        model.Entity<WeeklyNote>(note =>
        {
            note.ToTable("weekly_notes");
            note.Property(n => n.WentWell).HasMaxLength(WeeklyNote.MaxLength);
            note.Property(n => n.ToChange).HasMaxLength(WeeklyNote.MaxLength);
            note.HasIndex(UserIdColumn, nameof(WeeklyNote.WeekStart)).IsUnique();
        });

        model.Entity<DiaryEntry>(entry =>
        {
            entry.ToTable("diary_entries");
            entry.Property(e => e.Text).HasMaxLength(DiaryEntry.MaxLength);
            entry.HasIndex(UserIdColumn, nameof(DiaryEntry.Day)).IsUnique();
        });

        model.Entity<AssistantSettings>(settings =>
        {
            settings.ToTable("assistant_settings");
            settings.Property(s => s.AboutMe).HasMaxLength(Domain.Assistant.AssistantSettings.MaxAboutMe);
            settings.Property(s => s.Personality).HasMaxLength(Domain.Assistant.AssistantSettings.MaxPersonality);
        });

        model.Entity<DailyBriefing>(briefing =>
        {
            briefing.ToTable("daily_briefings");
            briefing.Property(b => b.Text).HasMaxLength(DailyBriefing.MaxLength);
            briefing.HasIndex(UserIdColumn, nameof(DailyBriefing.Day)).IsUnique();
        });

        // A plan is saved and loaded as one unit, so its items live in a jsonb column on the
        // plan's row instead of their own table. Ignore stops EF treating PlanItem as a table.
        model.Ignore<PlanItem>();
        model.Entity<Plan>(plan =>
        {
            plan.ToTable("plans");
            plan.Property(p => p.Items)
                .HasColumnType("jsonb")
                .HasConversion(
                    items => JsonSerializer.Serialize(items, Json),
                    json => JsonSerializer.Deserialize<List<PlanItem>>(json, Json) ?? new List<PlanItem>(),
                    new ValueComparer<List<PlanItem>>(
                        (a, b) => JsonSerializer.Serialize(a, Json) == JsonSerializer.Serialize(b, Json),
                        items => JsonSerializer.Serialize(items, Json).GetHashCode(),
                        items => JsonSerializer.Deserialize<List<PlanItem>>(JsonSerializer.Serialize(items, Json), Json)!));
        });

        model.Entity<AppUser>(u =>
        {
            u.ToTable("app_users");
            u.Property(x => x.Email).HasMaxLength(AppUser.MaxEmail);
            u.Property(x => x.Name).HasMaxLength(AppUser.MaxName);
            // Stored as "owner" / "self", as it was before it became an enum.
            u.Property(x => x.GeminiKeySetBy).HasMaxLength(16)
                .HasConversion(v => v!.Value.ToString().ToLowerInvariant(), v => (KeySource?)Enum.Parse<KeySource>(v, true));
            u.HasIndex(x => x.Email).IsUnique();
        });
    }

    private void PerPerson<T>(ModelBuilder model, string? key = "Id") where T : class
    {
        var entity = model.Entity<T>();
        entity.Property<Guid>(UserIdColumn);
        if (key is not null) entity.HasKey(UserIdColumn, key);
        else entity.HasIndex(UserIdColumn);
        entity.HasQueryFilter(e => EF.Property<Guid>(e, UserIdColumn) == CurrentUserId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampOwner();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        StampOwner();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    // New rows belong to the request's person. Viewing someone else is read-only, so nothing new is
    // written for them (the middleware refuses those requests before they get here).
    private void StampOwner()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added && e.Metadata.FindProperty(UserIdColumn) is not null))
        {
            var column = entry.Property(UserIdColumn);
            if ((Guid)column.CurrentValue! == Guid.Empty) column.CurrentValue = CurrentUserId;
        }
    }
}
