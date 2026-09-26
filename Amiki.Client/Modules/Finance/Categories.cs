using MudBlazor;

namespace Amiki.Modules.Finance;

public sealed record Category(string Name, string Icon, TxKind Kind);

public static class Categories
{
    public const string OtherExpense = "Other";
    public const string OtherIncome = "Other income";
    public const string Fees = "Fees";
    /// <summary>Corrections from balance checks: bring Amiki in line with reality, not real spending or income.</summary>
    public const string BalanceFix = "Balance fix";

    /// <summary>Corrections are left out of spending/income totals and charts (they're still in the list and history).</summary>
    public static bool IsCorrection(string category) => category == BalanceFix;

    public static readonly IReadOnlyList<Category> All =
    [
        new("Food", Icons.Material.Outlined.Restaurant, TxKind.Expense),
        new("Transport", Icons.Material.Outlined.DirectionsCar, TxKind.Expense),
        new("School", Icons.Material.Outlined.School, TxKind.Expense),
        new("Load & data", Icons.Material.Outlined.PhoneAndroid, TxKind.Expense),
        new("Bills", Icons.Material.Outlined.ReceiptLong, TxKind.Expense),
        new("Shopping", Icons.Material.Outlined.ShoppingBag, TxKind.Expense),
        new("Health", Icons.Material.Outlined.LocalHospital, TxKind.Expense),
        new("Fun", Icons.Material.Outlined.SportsEsports, TxKind.Expense),
        new(Fees, Icons.Material.Outlined.Toll, TxKind.Expense),
        new(OtherExpense, Icons.Material.Outlined.MoreHoriz, TxKind.Expense),
        new(BalanceFix, Icons.Material.Outlined.FactCheck, TxKind.Expense),

        new("Allowance", Icons.Material.Outlined.Savings, TxKind.Income),
        new("Salary", Icons.Material.Outlined.Work, TxKind.Income),
        new("Gift", Icons.Material.Outlined.CardGiftcard, TxKind.Income),
        new("Refund", Icons.Material.Outlined.Replay, TxKind.Income),
        new(OtherIncome, Icons.Material.Outlined.AttachMoney, TxKind.Income),
        new(BalanceFix, Icons.Material.Outlined.FactCheck, TxKind.Income),
    ];

    public static Category Get(string name) => All.FirstOrDefault(c => c.Name == name) ?? All.First(c => c.Name == OtherExpense);

    public static IEnumerable<Category> Of(TxKind kind) => All.Where(c => c.Kind == kind);

    public static string DefaultFor(TxKind kind) => kind == TxKind.Income ? "Allowance" : "Food";
}
