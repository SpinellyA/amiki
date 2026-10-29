namespace Amiki.Tests.Wishlist;

public class WishItemTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 18, 0, 0);

    private static WishItem Tablet(decimal price = 4500, WishStatus status = WishStatus.Want, decimal saved = 0, int rank = 0) =>
        new() { Name = "Drawing tablet", Price = price, Status = status, Saved = saved, Rank = rank, CreatedAt = Now.AddDays(-30) };

    private static WishlistFund Fund(decimal amount) => new WishlistFund().SetTo(amount, Now);

    // ---------- Putting money toward things ----------

    [Fact]
    public void Putting_money_toward_an_item_moves_it_from_the_pool_and_never_more_than_needed()
    {
        var change = Fund(5000).PutToward(Tablet(4500), 3000, Now);
        Assert.Equal((2000m, 3000m), (change.Fund.Amount, change.Item.Saved));

        var tooMuch = change.Fund.PutToward(change.Item, 9999, Now); // only 1,500 still needed
        Assert.Equal((500m, 4500m), (tooMuch.Fund.Amount, tooMuch.Item.Saved));

        var poolLimited = Fund(100).PutToward(Tablet(4500), 1000, Now);
        Assert.Equal((0m, 100m), (poolLimited.Fund.Amount, poolLimited.Item.Saved));
    }

    [Fact]
    public void Taking_money_back_returns_it_to_the_pool()
    {
        var back = Fund(0).PutToward(Tablet(saved: 3000), -1000, Now);
        Assert.Equal((1000m, 2000m), (back.Fund.Amount, back.Item.Saved));

        var released = Fund(0).Release(Tablet(saved: 3000), Now);
        Assert.Equal((3000m, 0m), (released.Fund.Amount, released.Item.Saved));
    }

    [Fact]
    public void Buying_spends_the_items_own_money_first_then_the_pool()
    {
        var expense = Guid.NewGuid();

        var bought = Fund(5000).Buy(Tablet(4500, WishStatus.Saving, saved: 3000), paid: 4500, expense, Now);
        Assert.Equal(3500, bought.Fund.Amount); // 1,500 more from the pool
        Assert.Equal((WishStatus.Bought, expense), (bought.Item.Status, bought.Item.TransactionId!.Value));
        Assert.False(bought.Item.IsOpen);

        var onSale = Fund(1000).Buy(Tablet(4500, WishStatus.Saving, saved: 4500), paid: 4000, expense, Now);
        Assert.Equal(1500, onSale.Fund.Amount); // the 500 it didn't need goes back

        var short_ = Fund(200).Buy(Tablet(4500), paid: 4500, expense, Now);
        Assert.Equal(0, short_.Fund.Amount); // never below zero
    }

    [Fact]
    public void Moving_back_from_bought_forgets_the_purchase()
    {
        var bought = Fund(0).Buy(Tablet(), 4500, Guid.NewGuid(), Now).Item;

        var reopened = bought.WithStatus(WishStatus.Saving, Now.AddDays(1));

        Assert.Equal((WishStatus.Saving, (DateTime?)null, (Guid?)null), (reopened.Status, reopened.BoughtAt, reopened.TransactionId));
    }

    // ---------- The summary ----------

    [Fact]
    public void Items_are_weighed_against_their_own_money_plus_the_pool()
    {
        var afford = Tablet(4500, saved: 3000).Against(pool: 2000);
        var shortOf = Tablet(4500, saved: 1000).Against(pool: 2000);

        Assert.Equal((true, 500m), (afford.CanAfford, afford.LeftAfter));
        Assert.Equal((false, 1500m), (shortOf.CanAfford, shortOf.Short));
    }

    [Fact]
    public void The_summary_counts_only_money_still_in_your_accounts()
    {
        var items = new[] { Tablet(4500, WishStatus.Saving, saved: 3000), Tablet(800, WishStatus.Want) };

        var fine = new WishlistSummary(items, fund: 2000, balance: 50000);
        Assert.Equal((5000m, 2000m, false), (fine.SetAside, fine.Pool, fine.Overcommitted));

        var spentElsewhere = new WishlistSummary(items, fund: 2000, balance: 3500);
        Assert.True(spentElsewhere.Overcommitted);
        Assert.Equal(500, spentElsewhere.Pool); // 3,500 in the accounts, 3,000 of it already put toward the tablet

        var overdrawn = new WishlistSummary(items, fund: 2000, balance: -100);
        Assert.Equal(0, overdrawn.Pool);
    }

    [Fact]
    public void It_counts_how_many_of_your_top_items_the_pool_finishes()
    {
        var items = new[]
        {
            Tablet(1000, WishStatus.Saving, saved: 500, rank: 0), // needs 500
            Tablet(2000, WishStatus.Saving, rank: 1),             // needs 2,000
            Tablet(100, WishStatus.Saving, rank: 2),              // cheap, but third in your order
        };

        Assert.Equal(1, new WishlistSummary(items, fund: 2000, balance: 10000).TopAffordable);
        Assert.Equal(3, new WishlistSummary(items, fund: 2600, balance: 10000).TopAffordable);
        Assert.Equal(items[0], new WishlistSummary(items, fund: 0, balance: 10000).Closest);
    }

    // ---------- Pace, prices, cooling off, income ----------

    [Fact]
    public void A_target_date_becomes_a_weekly_amount()
    {
        var item = Tablet(4500, WishStatus.Saving, saved: 500);
        item.TargetDate = Now.Date.AddDays(28);

        var pace = item.PaceOn(Now.Date)!.Value;

        Assert.Equal((4, 1000m, false), (pace.WeeksLeft, pace.PerWeek, pace.IsLate));
        Assert.True(item.PaceOn(Now.Date.AddDays(30))!.Value.IsLate);
        Assert.Null(Tablet().PaceOn(Now.Date)); // no date, no pace
    }

    [Fact]
    public void A_new_price_keeps_the_old_one_in_its_history()
    {
        var cheaper = Tablet(4999).Repriced(4500, Now);

        Assert.Equal((4500m, 4999m), (cheaper.Price, cheaper.PreviousPrice!.Value));
        Assert.Empty(cheaper.Repriced(4500, Now).PriceHistory.Skip(1)); // the same price isn't a change
    }

    [Fact]
    public void Brand_new_wants_get_a_pause_and_old_ones_a_question()
    {
        var justAdded = new WishItem { Name = "Mouse", Price = 1200, CreatedAt = Now.AddHours(-3) };

        Assert.True(justAdded.IsFreshWant(Now));
        Assert.False(justAdded.HasCooledOff(Now));
        Assert.True(Tablet().HasCooledOff(Now)); // wanted for 30 days
        Assert.False(Tablet(status: WishStatus.Saving).HasCooledOff(Now)); // already decided
    }

    [Fact]
    public void A_share_of_income_goes_to_wishlist_money_when_the_rule_is_on()
    {
        Assert.Equal(0, Fund(0).ShareOf(1500));
        Assert.Equal(150, Fund(0).WithIncomeShare(10, Now).ShareOf(1500));
        Assert.NotNull(new WishlistFund { IncomeShare = 0 }.Validate());
        Assert.NotNull(new WishlistFund { IncomeShare = 101 }.Validate());
    }

    // ---------- Checks ----------

    [Theory]
    [InlineData("", 100, null, "Give it a name.")]
    [InlineData("Tablet", -1, null, "A price can't be negative.")]
    [InlineData("Tablet", 100, "shopee.ph/item", "A link has to start with http:// or https://.")]
    [InlineData("Tablet", 100, "javascript:alert(1)", "A link has to start with http:// or https://.")]
    [InlineData("Tablet", 100, "https://shopee.ph/item", null)]
    public void Items_are_checked_before_saving(string name, decimal price, string? link, string? problem) =>
        Assert.Equal(problem, new WishItem { Name = name, Price = price, Link = link }.Validate());

    [Fact]
    public void An_open_item_cant_have_more_put_toward_it_than_it_costs() =>
        Assert.Equal("More is put toward it than it costs.", Tablet(100, saved: 200).Validate());

    [Fact]
    public void Only_a_bought_item_points_at_an_expense() =>
        Assert.NotNull(new WishItem { Name = "Tablet", Price = 1, TransactionId = Guid.NewGuid() }.Validate());
}
