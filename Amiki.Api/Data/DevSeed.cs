using Amiki.Modules.Finance;
using Amiki.Modules.Ideas;
using Amiki.Modules.Inbox;
using Amiki.Modules.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Data;

/// <summary>
/// Sample data for a brand-new Development database, so every screen has something on it.
/// Runs only when the database is empty, and never outside Development.
/// </summary>
public static class DevSeed
{
    public static async Task RunAsync(AmikiDb db)
    {
        if (await db.Tasks.AnyAsync() || await db.Inbox.AnyAsync() || await db.Transactions.AnyAsync()) return;

        var today = DateTime.Today;
        var now = DateTime.Now;

        db.Tasks.AddRange(Tasks(today, now));
        db.Ideas.AddRange(Ideas(now));
        db.Inbox.AddRange(Inbox(now));
        var transactions = Transactions(today);
        db.Transactions.AddRange(transactions);

        var accounts = await db.Accounts.ToListAsync();
        var balance = accounts.Sum(a => a.OpeningBalance) + transactions.Sum(t => t.Signed);
        db.Plans.AddRange(Plans(today, balance));

        await db.SaveChangesAsync();
    }

    private static IEnumerable<TaskItem> Tasks(DateTime today, DateTime now)
    {
        TaskItem Open(string title, string? project, int? dueIn, Priority p = Priority.None, string? notes = null) =>
            new() { Title = title, Project = project, Due = dueIn is { } d ? today.AddDays(d) : null, Priority = p, Notes = notes };
        TaskItem Done(string title, string? project, int daysAgo) =>
            new() { Title = title, Project = project, CompletedAt = now.AddDays(-daysAgo).AddMinutes(-30), CreatedAt = today.AddDays(-daysAgo - 2) };

        return
        [
            Open("Finish lab 3 write-up", "cmsc128", 0, Priority.High, "Include the complexity table from the handout."),
            Open("Reply to adviser about venue", "org", -1, Priority.Medium),
            Open("Buy groceries", null, 0, Priority.Low),
            Open("Review for long exam", "math55", 2, Priority.High),
            Open("Read chapter 4: scheduling", "cmsc125", 3),
            Open("Pick a host for Scholaris staging", "scholaris", 4, Priority.Medium),
            Open("Pay internet bill", null, 6, Priority.Medium),
            Open("Sketch the Amiki module list", "amiki", null),
            Open("Test Google Classroom OAuth on up.edu.ph", "amiki", null, Priority.High,
                 "30-minute spike. If the Workspace admin blocks it, the classroom adapter is dead on arrival."),
            Done("Fix skin upload bug", "ponyfolio", 0),
            Done("Submit lab 2", "cmsc128", 1),
            Done("Update portfolio projects", null, 1),
            Done("Laundry", null, 2),
            Done("Email professor re: excuse letter", "cmsc125", 4),
            Done("Groceries", null, 5),
            Done("Problem set 3", "math55", 5),
            Done("Org meeting minutes", "org", 6),
        ];
    }

    private static IEnumerable<Idea> Ideas(DateTime now) =>
    [
        Idea.Parse("Pony Town skin tags that auto-suggest from the palette #ponyfolio", now.AddDays(-12)),
        Idea.Parse("Org site: let officers post announcements without touching code #upcsg", now.AddDays(-9)),
        Idea.Parse("Streak freeze tokens you earn by finishing a hard week #amiki", now.AddDays(-6)),
        Idea.Parse("Compare screen time against sleep on the same chart #amiki #health", now.AddDays(-3)),
        Idea.Parse("Thesis angle: offline-first sync for rural sari-sari stores #thesis #superledger", now.AddDays(-2)),
        Idea.Parse("Study group that shares one Pomodoro timer #school", now.AddDays(-1)),
    ];

    private static IEnumerable<InboxItem> Inbox(DateTime now)
    {
        InboxItem Ago(string text, int minutes) => new() { Text = text, CapturedAt = now.AddMinutes(-minutes) };
        return
        [
            Ago("prof said quiz next thu covers ch 5-6 #cmsc125 @thu", 290),
            Ago("group: I'm doing the ERD, due before the monday meeting #cmsc128 @mon", 200),
            Ago("ask Mika if we're splitting the slides per section", 150),
            Ago("app idea: auto-build a study schedule from exam dates #amiki", 75),
            Ago("buy index cards", 20),
            Ago("return the org's extension cord to the SC office #org", 60 * 24 + 30),
        ];
    }

    private static List<Transaction> Transactions(DateTime today)
    {
        Transaction Out(int daysAgo, decimal amount, string category, string note, string account = "Cash") =>
            new() { Date = today.AddDays(-daysAgo), Amount = amount, Kind = TxKind.Expense, Category = category, Note = note, Account = account };
        Transaction In(int daysAgo, decimal amount, string category, string note, string account = "Cash") =>
            new() { Date = today.AddDays(-daysAgo), Amount = amount, Kind = TxKind.Income, Category = category, Note = note, Account = account };

        return
        [
            Out(0, 85, "Food", "Lunch at the canteen"), Out(0, 26, "Transport", "Jeep fare"),
            Out(1, 120, "Food", "Milk tea", "GCash"), Out(1, 45, "School", "Printed thesis draft"),
            Out(2, 500, "Transport", "Gas"),
            In(3, 4000, "Allowance", "Allowance from Lola", "GCash"), Out(3, 186, "Transport", "Grab home", "GCash"),
            Out(4, 99, "Load & data", "Globe load", "GCash"),
            Out(5, 95, "Food", "Lunch"), Out(5, 70, "School", "Index cards and bond paper"),
            In(6, 1500, "Allowance", "Weekly baon from Mama"),
            Out(7, 350, "Fun", "Movie with the org"),
            Out(8, 150, "Food", "Dinner"),
            Out(9, 500, "Bills", "Internet bill share", "GCash"),
            Out(10, 500, "Transport", "Gas"),
            Out(11, 80, "Food", "Lunch"),
            Out(12, 459, "Shopping", "USB hub", "GCash"),
            In(13, 1500, "Allowance", "Weekly baon from Mama"),
            Out(14, 110, "Food", "Coffee"),
            Out(15, 180, "Health", "Vitamins"),
            Out(16, 60, "School", "Photocopied readings"),
            Out(17, 90, "Food", "Lunch"),
            Out(18, 450, "Transport", "Gas"),
            In(20, 1500, "Allowance", "Weekly baon from Mama"),
            Out(21, 230, "Food", "Jollibee with blockmates"),
            Out(23, 3000, "School", "Tuition installment", "BPI"),
            In(24, 120, "Refund", "Shopee refund", "GCash"),
            Out(25, 50, "Load & data", "Load"),
            Out(26, 140, "Food", "Dinner"),
            In(27, 1500, "Allowance", "Weekly baon from Mama"),
            In(28, 600, "Other income", "Sold old calculator"),
        ];
    }

    private static IEnumerable<Plan> Plans(DateTime today, decimal balance)
    {
        var nextMonth = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        PlanItem Out(string name, decimal amount) => new() { Name = name, Amount = amount, Date = today };
        PlanItem In(string name, decimal amount, DateTime when) => new() { Name = name, Kind = TxKind.Income, Amount = amount, Date = when };

        var july = new DateTime(today.Year, 7, 3);
        return
        [
            new()
            {
                Name = "Gaming laptop", StartBalance = balance, StartDate = today, CreatedAt = DateTime.Now,
                Items = [Out("New laptop", 35000), Out("Mouse", 1200), Out("Monthsary date", 1500), Out("Something something", 1000), In($"{nextMonth:MMMM} money", 10000, nextMonth)],
            },
            new()
            {
                Name = "Budget laptop", StartBalance = balance, StartDate = today, CreatedAt = DateTime.Now.AddSeconds(1),
                Items = [Out("New laptop", 25000), Out("Mouse", 650), Out("Monthsary date", 1500), Out("Something something", 1000), In($"{nextMonth:MMMM} money", 10000, nextMonth)],
            },
            new()
            {
                Name = "Phone upgrade", StartBalance = 14200, StartDate = july, CreatedAt = july, UpdatedAt = july.AddHours(20), ArchivedAt = july.AddDays(12),
                Items =
                [
                    new() { Name = "New phone", Amount = 16000, Date = july },
                    new() { Name = "Phone case", Amount = 450, Date = july },
                    new() { Name = "August money", Kind = TxKind.Income, Amount = 8000, Date = july.AddMonths(1).AddDays(-2) },
                ],
            },
        ];
    }
}
