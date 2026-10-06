using Amiki.Modules.Ideas;
using Amiki.Modules.Tasks;

namespace Amiki.Tests.Ideas;

public class IdeaStatusTests
{
    private static TaskItem Open() => new() { Title = "Open" };
    private static TaskItem Done() => new() { Title = "Done", CompletedAt = DateTime.Now };

    [Fact]
    public void New_ideas_start_parked()
    {
        Assert.Equal(IdeaStatus.Parked, Idea.Parse("Pomodoro for study groups #amiki").Status);
    }

    [Fact]
    public void An_idea_with_no_tasks_keeps_whatever_status_you_gave_it()
    {
        Assert.Null(Idea.StatusFromTasks([]));
    }

    [Fact]
    public void An_idea_with_an_open_task_is_in_progress()
    {
        Assert.Equal(IdeaStatus.InProgress, Idea.StatusFromTasks([Open()]));
    }

    [Fact]
    public void An_idea_is_done_only_when_all_its_tasks_are()
    {
        Assert.Equal(IdeaStatus.InProgress, Idea.StatusFromTasks([Done(), Open()]));
        Assert.Equal(IdeaStatus.Done, Idea.StatusFromTasks([Done(), Done()]));
    }

    [Fact]
    public void Clone_keeps_status_and_copies_tags()
    {
        var idea = new Idea { Text = "x", Tags = ["a"], Status = IdeaStatus.InProgress };

        var copy = idea.Clone();
        copy.Tags.Add("b");

        Assert.Equal(IdeaStatus.InProgress, copy.Status);
        Assert.Equal(idea.Id, copy.Id);
        Assert.Single(idea.Tags);
    }
}
