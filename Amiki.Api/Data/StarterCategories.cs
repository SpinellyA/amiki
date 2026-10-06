using Amiki.Modules.Finance;

namespace Amiki.Api.Data;

/// <summary>
/// The categories a new database starts with (the list the app used to have built in). Ids and
/// dates are fixed so the migration that adds them never changes.
/// </summary>
public static class StarterCategories
{
    private static readonly DateTime Added = new(2026, 10, 6);

    public static readonly Category[] All =
    [
        Spending("1f0c8a01-0000-4000-8000-000000000001", "Food", "Restaurant"),
        Spending("1f0c8a01-0000-4000-8000-000000000002", "Transport", "DirectionsCar"),
        Spending("1f0c8a01-0000-4000-8000-000000000003", "School", "School"),
        Spending("1f0c8a01-0000-4000-8000-000000000004", "Load & data", "PhoneAndroid"),
        Spending("1f0c8a01-0000-4000-8000-000000000005", "Bills", "ReceiptLong"),
        Spending("1f0c8a01-0000-4000-8000-000000000006", "Shopping", "ShoppingBag"),
        Spending("1f0c8a01-0000-4000-8000-000000000007", "Health", "LocalHospital"),
        Spending("1f0c8a01-0000-4000-8000-000000000008", "Fun", "SportsEsports"),
        Spending("1f0c8a01-0000-4000-8000-000000000009", Category.Fees, "Toll"),
        Spending("1f0c8a01-0000-4000-8000-00000000000a", Category.OtherExpense, "MoreHoriz"),
        Spending("1f0c8a01-0000-4000-8000-00000000000b", Category.BalanceFix, "FactCheck"),

        Income("1f0c8a01-0000-4000-8000-000000000101", "Allowance", "Savings"),
        Income("1f0c8a01-0000-4000-8000-000000000102", "Salary", "Work"),
        Income("1f0c8a01-0000-4000-8000-000000000103", "Gift", "CardGiftcard"),
        Income("1f0c8a01-0000-4000-8000-000000000104", "Refund", "Replay"),
        Income("1f0c8a01-0000-4000-8000-000000000105", Category.OtherIncome, "AttachMoney"),
        Income("1f0c8a01-0000-4000-8000-000000000106", Category.BalanceFix, "FactCheck"),
    ];

    private static Category Spending(string id, string name, string icon) =>
        new() { Id = Guid.Parse(id), Name = name, Kind = TxKind.Expense, Icon = icon, CreatedAt = Added };

    private static Category Income(string id, string name, string icon) =>
        new() { Id = Guid.Parse(id), Name = name, Kind = TxKind.Income, Icon = icon, CreatedAt = Added };
}
