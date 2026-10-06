using Amiki.Modules.Tasks;

namespace Amiki.Tests.Tasks;

public class QuickAddTests
{
    [Fact]
    public void A_task_without_a_day_is_due_today()
    {
        var task = QuickAdd.Parse("Buy index cards");

        Assert.Equal(DateTime.Today, task.Due);
    }

    [Fact]
    public void A_task_without_a_day_takes_the_given_default()
    {
        var friday = DateTime.Today.AddDays(3);

        var task = QuickAdd.Parse("Buy index cards", defaultDue: friday);

        Assert.Equal(friday, task.Due);
    }

    [Fact]
    public void An_explicit_day_wins_over_the_default()
    {
        var task = QuickAdd.Parse("Submit lab @tomorrow #cmsc128", defaultDue: DateTime.Today.AddDays(5));

        Assert.Equal(DateTime.Today.AddDays(1), task.Due);
        Assert.Equal("cmsc128", task.Project);
        Assert.Equal("Submit lab", task.Title);
    }
}
