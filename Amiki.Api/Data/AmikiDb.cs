using System.Text.Json;
using Amiki.Core;
using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Inbox;
using Amiki.Modules.Tasks;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Amiki.Api.Data;

public sealed class AmikiDb(DbContextOptions<AmikiDb> options) : DbContext(options), IDataProtectionKeyContext
{
    /// <summary>Sign-in cookie encryption keys; kept here so restarts don't sign you out.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<InboxItem> Inbox => Set<InboxItem>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Idea> Ideas => Set<Idea>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<BalanceCheck> BalanceChecks => Set<BalanceCheck>();
    public DbSet<AuditEntry> AuditLog => Set<AuditEntry>();
    public DbSet<Plan> Plans => Set<Plan>();

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
        model.Entity<InboxItem>().ToTable("inbox_items");

        model.Entity<TaskItem>(task =>
        {
            task.ToTable("tasks");
            task.Property(t => t.Priority).HasConversion<string>().HasMaxLength(16);
        });

        // Tags map to a Postgres text[] column.
        model.Entity<Idea>().ToTable("ideas");

        model.Entity<Transaction>(tx =>
        {
            tx.ToTable("transactions");
            tx.Property(t => t.Kind).HasConversion<string>().HasMaxLength(16);
            tx.HasIndex(t => t.Date);
        });

        // Start at zero: the first balance check on each account sets its real amount (as a
        // labeled correction), instead of made-up opening balances skewing every total.
        model.Entity<Account>(account =>
        {
            account.ToTable("accounts");
            account.HasKey(a => a.Name);
            account.HasData(new Account("Cash", 0), new Account("GCash", 0), new Account("Maya", 0), new Account("Landbank", 0));
        });

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
    }
}
