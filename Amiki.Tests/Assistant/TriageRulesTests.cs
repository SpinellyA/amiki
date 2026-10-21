using Amiki.Modules.Assistant;
using Amiki.Modules.Tasks;

namespace Amiki.Tests.Assistant;

public class TriageRulesTests
{
    private static readonly TriageNote Note = new(Guid.NewGuid(), "bought gas 500");

    private static readonly TriageContext Context = new(
        Today: new DateTime(2026, 10, 21),
        Accounts: ["Cash", "GCash", "Landbank", "Maribank"],
        ExpenseCategories: ["Food", "Other", "Transport"],
        IncomeCategories: ["Allowance", "Other income"],
        Projects: ["cmsc125"],
        Examples: []);

    private static TriageSuggestion Clean(TriageSuggestion raw) => TriageRules.Clean(raw, Note, Context);

    [Fact]
    public void Money_uses_the_real_names_whatever_the_case()
    {
        var s = Clean(new(Note.Id, TriageKind.Expense, "Gas", Amount: 500, Category: "transport", Account: "gcash"));

        Assert.Equal(("Transport", "GCash"), (s.Category, s.Account));
    }

    [Fact]
    public void A_made_up_category_becomes_Other_and_an_unknown_account_becomes_Cash()
    {
        var s = Clean(new(Note.Id, TriageKind.Expense, "Gas", Amount: 500, Category: "Fuel", Account: "BDO"));

        Assert.Equal(("Other", "Cash"), (s.Category, s.Account));
    }

    [Fact]
    public void Income_falls_back_to_Other_income()
    {
        var s = Clean(new(Note.Id, TriageKind.Income, "Sold calculator", Amount: 600, Category: "Sales"));

        Assert.Equal("Other income", s.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-20)]
    public void Money_without_a_real_amount_is_unsure(int? amount)
    {
        var s = Clean(new(Note.Id, TriageKind.Expense, "Gas", Amount: amount, Category: "Transport"));

        Assert.Equal(TriageKind.Unsure, s.Kind);
        Assert.Null(s.Amount);
        Assert.Null(s.Category);
    }

    [Fact]
    public void Tasks_keep_only_task_fields_and_tidy_the_project()
    {
        var s = Clean(new(Note.Id, TriageKind.Task, "Quiz ch 5-6", Due: new DateTime(2026, 10, 23, 9, 0, 0),
            Project: "#CMSC125", Priority: Priority.High, Amount: 50, Category: "Food"));

        Assert.Equal(new DateTime(2026, 10, 23), s.Due);
        Assert.Equal("cmsc125", s.Project);
        Assert.Equal(Priority.High, s.Priority);
        Assert.Null(s.Amount);
        Assert.Null(s.Category);
    }

    [Fact]
    public void An_empty_title_falls_back_to_the_note_and_long_ones_are_cut()
    {
        Assert.Equal("bought gas 500", Clean(new(Note.Id, TriageKind.Idea, "  ")).Title);
        Assert.True(Clean(new(Note.Id, TriageKind.Idea, new string('a', 500))).Title.Length <= TriageRules.MaxTitleLength + 1);
    }

    [Fact]
    public void The_note_id_always_comes_from_the_note()
    {
        Assert.Equal(Note.Id, Clean(new(Guid.NewGuid(), TriageKind.Idea, "x")).NoteId);
    }
}
