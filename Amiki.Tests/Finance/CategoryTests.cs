using Amiki.Modules.Finance;

namespace Amiki.Tests.Finance;

public class CategoryTests
{
    [Theory]
    [InlineData("", "Give the category a name.")]
    [InlineData("   ", "Give the category a name.")]
    [InlineData(" Snacks", "The name can't start or end with a space.")]
    public void Validate_rejects_bad_names(string name, string expected)
    {
        var category = new Category { Name = name, Icon = "Label" };

        Assert.Equal(expected, category.Validate());
    }

    [Fact]
    public void Validate_rejects_names_over_the_limit()
    {
        var category = new Category { Name = new string('a', Category.MaxNameLength + 1), Icon = "Label" };

        Assert.NotNull(category.Validate());
    }

    [Fact]
    public void Validate_accepts_a_normal_category()
    {
        var category = new Category { Name = "Snacks", Kind = TxKind.Expense, Icon = "Fastfood" };

        Assert.Null(category.Validate());
    }

    [Theory]
    [InlineData(Category.OtherExpense)]
    [InlineData(Category.OtherIncome)]
    [InlineData(Category.Fees)]
    [InlineData(Category.BalanceFix)]
    public void Built_in_categories_cannot_be_renamed(string name)
    {
        var stored = new Category { Name = name };
        var renamed = stored.Clone();
        renamed.Name = "Something else";

        Assert.NotNull(Category.CheckReplace(stored, renamed));
    }

    [Fact]
    public void Built_in_categories_can_change_their_icon()
    {
        var stored = new Category { Name = Category.OtherExpense, Icon = "MoreHoriz" };
        var changed = stored.Clone();
        changed.Icon = "Star";

        Assert.Null(Category.CheckReplace(stored, changed));
    }

    [Fact]
    public void Your_own_categories_can_be_renamed()
    {
        var stored = new Category { Name = "Snacks" };
        var renamed = stored.Clone();
        renamed.Name = "Treats";

        Assert.Null(Category.CheckReplace(stored, renamed));
    }

    [Fact]
    public void A_category_cannot_switch_between_spending_and_income()
    {
        var stored = new Category { Name = "Snacks", Kind = TxKind.Expense };
        var switched = stored.Clone();
        switched.Kind = TxKind.Income;

        Assert.NotNull(Category.CheckReplace(stored, switched));
    }

    [Fact]
    public void Deleted_categories_fall_back_to_the_matching_catch_all()
    {
        Assert.Equal(Category.OtherExpense, Category.FallbackFor(TxKind.Expense));
        Assert.Equal(Category.OtherIncome, Category.FallbackFor(TxKind.Income));
    }
}
