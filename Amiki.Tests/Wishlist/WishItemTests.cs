namespace Amiki.Tests.Wishlist;

public class WishItemTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 18, 0, 0);

    private static WishItem Tablet(decimal price = 4500, WishStatus status = WishStatus.Want) =>
        new() { Name = "Drawing tablet", Price = price, Status = status };

    [Fact]
    public void It_says_whether_your_balance_covers_it_and_by_how_much()
    {
        var afford = Tablet(4500).Against(5000);
        var tooMuch = Tablet(4500).Against(3000);

        Assert.Equal((true, 500m, 0m), (afford.CanAfford, afford.LeftAfter, afford.Short));
        Assert.Equal((false, 1500m), (tooMuch.CanAfford, tooMuch.Short));
        Assert.Equal(3000.0 / 4500, tooMuch.Covered, precision: 6);
        Assert.Equal(0, Tablet(4500).Against(-200).Covered); // an overdrawn balance covers nothing
    }

    [Fact]
    public void Buying_links_the_expense_and_moving_back_forgets_it()
    {
        var expense = Guid.NewGuid();

        var bought = Tablet().BoughtWith(expense, Now);
        var reopened = bought.WithStatus(WishStatus.Saving, Now.AddDays(1));

        Assert.Equal((WishStatus.Bought, Now, expense), (bought.Status, bought.BoughtAt!.Value, bought.TransactionId!.Value));
        Assert.False(bought.IsOpen);
        Assert.Equal((WishStatus.Saving, (DateTime?)null, (Guid?)null), (reopened.Status, reopened.BoughtAt, reopened.TransactionId));
        Assert.Null(reopened.Validate());
    }

    [Fact]
    public void The_summary_weighs_the_wishlist_against_wishlist_money_not_the_whole_balance()
    {
        var summary = new WishlistSummary(
        [
            Tablet(4500, WishStatus.Saving),
            Tablet(800, WishStatus.Saving),
            Tablet(20000, WishStatus.Want),
            Tablet(999, WishStatus.Bought),
            Tablet(50, WishStatus.Skipped),
        ], fund: 10000, balance: 50000);

        Assert.Equal(5300, summary.SavingFor);
        Assert.Equal(4700, summary.AfterSaving.LeftAfter);
        Assert.False(summary.AfterEverything.CanAfford); // 25,300 > 10,000 set aside, though the balance could cover it
        Assert.Equal(15300, summary.AfterEverything.Short);
        Assert.Equal(50000, summary.Balance);
        Assert.False(summary.FundExceedsBalance);
        Assert.Equal(3, summary.OpenCount);
    }

    [Fact]
    public void Buying_spends_from_wishlist_money_but_never_below_zero()
    {
        var fund = new WishlistFund().SetTo(5000, Now);

        Assert.Equal(500, fund.Spend(4500, Now).Amount);
        Assert.Equal(0, fund.Spend(9000, Now).Amount);
        Assert.Null(fund.Validate());
        Assert.NotNull(fund.SetTo(-1, Now).Validate());
        Assert.NotNull(new WishlistFund { Id = Guid.NewGuid() }.Validate()); // there's only one per person
    }

    [Theory]
    [InlineData("", 100, null, "Give it a name.")]
    [InlineData("Tablet", -1, null, "A price can't be negative.")]
    [InlineData("Tablet", 100, "shopee.ph/item", "A link has to start with http:// or https://.")]
    [InlineData("Tablet", 100, "javascript:alert(1)", "A link has to start with http:// or https://.")]
    [InlineData("Tablet", 100, "https://shopee.ph/item", null)]
    public void Items_are_checked_before_saving(string name, decimal price, string? link, string? problem) =>
        Assert.Equal(problem, new WishItem { Name = name, Price = price, Link = link }.Validate());

    [Fact]
    public void Only_a_bought_item_points_at_an_expense() =>
        Assert.NotNull(new WishItem { Name = "Tablet", Price = 1, TransactionId = Guid.NewGuid() }.Validate());
}
