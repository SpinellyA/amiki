namespace Amiki.Tests.Finance;

public class LedgerTests
{
    private static readonly DateTime Monday = new(2026, 10, 19);

    private static readonly Account[] Accounts = [new("Cash", 100), new("GCash", 0)];

    private static Transaction Spent(decimal amount, string category = "Food", string account = "Cash", int day = 0) =>
        new() { Date = Monday.AddDays(day), Amount = amount, Kind = TxKind.Expense, Category = category, Account = account };

    private static Transaction Received(decimal amount, string account = "Cash", int day = 0) =>
        new() { Date = Monday.AddDays(day), Amount = amount, Kind = TxKind.Income, Category = "Allowance", Account = account };

    [Fact]
    public void A_balance_is_the_opening_amount_plus_everything_touching_the_account()
    {
        var moved = new Transfer { Date = Monday, From = "Cash", To = "GCash", Amount = 50, Fee = 15 };
        var ledger = new Ledger(Accounts, [Spent(30), Received(200)], [moved]);

        Assert.Equal(100 - 30 + 200 - 50 - 15, ledger.Balance("Cash"));
        Assert.Equal(50, ledger.Balance("GCash"));
        Assert.Equal(100 - 30 + 200 - 15, ledger.TotalBalance); // moving money between accounts costs only the fee
    }

    [Fact]
    public void Real_flows_leave_out_moves_but_count_transfer_fees()
    {
        var moved = new Transfer { Date = Monday.AddDays(1), From = "Cash", To = "GCash", Amount = 500, Fee = 15 };
        var ledger = new Ledger(Accounts, [Spent(80), Spent(40, day: 7)], [moved]);

        var flows = ledger.FlowsBetween(Monday, Monday.AddDays(7)).ToList();

        Assert.Equal([80m, 15m], flows.Select(f => f.Amount));
        var fee = flows.Single(f => f.Category == Category.Fees);
        Assert.Equal(("Cash", TxKind.Expense), (fee.Account, fee.Kind));
    }

    [Fact]
    public void Money_a_balance_check_found_missing_counts_as_spending()
    {
        var wentMissing = Spent(300, category: Category.BalanceFix);
        var foundExtra = Received(15000); // e.g. the first check on an account, setting its real amount
        foundExtra.Category = Category.BalanceFix;
        var ledger = new Ledger(Accounts, [Spent(80), wentMissing, foundExtra], []);

        var flows = ledger.FlowsBetween(Monday, Monday.AddDays(7)).ToList();

        Assert.Equal([80m, 300m], flows.Select(f => f.Amount));
        Assert.DoesNotContain(flows, f => f.Kind == TxKind.Income);
        Assert.Equal(100 - 80 - 300 + 15000, ledger.Balance("Cash")); // balances still include every correction
    }

    [Fact]
    public void Weeks_run_monday_to_sunday()
    {
        Assert.Equal(Monday, WeekRange.Of(Monday.AddDays(6).AddHours(23)).Start); // Sunday night
        Assert.Equal(Monday.AddDays(7), WeekRange.Of(Monday.AddDays(7)).Start);  // next Monday
    }
}
