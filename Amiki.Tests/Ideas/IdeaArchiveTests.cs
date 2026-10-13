using Amiki.Modules.Ideas;

namespace Amiki.Tests.Ideas;

public class IdeaArchiveTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 9, 0, 0);

    [Fact]
    public void Becoming_done_starts_the_countdown()
    {
        var done = new Idea { Text = "x" }.WithStatus(IdeaStatus.Done, Now);

        Assert.Equal(Now, done.DoneAt);
        Assert.False(done.ShouldAutoArchive(Now.AddDays(13)));
        Assert.True(done.ShouldAutoArchive(Now + Idea.ArchiveDoneAfter));
    }

    [Fact]
    public void Leaving_done_clears_the_countdown()
    {
        var reopened = new Idea { Text = "x" }.WithStatus(IdeaStatus.Done, Now).WithStatus(IdeaStatus.InProgress, Now.AddDays(1));

        Assert.Null(reopened.DoneAt);
        Assert.False(reopened.ShouldAutoArchive(Now.AddDays(30)));
    }

    [Fact]
    public void Only_done_ideas_archive_by_themselves()
    {
        var parked = new Idea { Text = "x", DoneAt = Now.AddDays(-30) };

        Assert.False(parked.ShouldAutoArchive(Now));
    }

    [Fact]
    public void Moving_an_archived_idea_out_of_done_brings_it_back()
    {
        var archived = new Idea { Text = "x" }.WithStatus(IdeaStatus.Done, Now).Archived(Now.AddDays(14));

        var active = archived.WithStatus(IdeaStatus.InProgress, Now.AddDays(20));

        Assert.True(archived.IsArchived);
        Assert.False(active.IsArchived);
    }

    [Fact]
    public void Restoring_a_done_idea_gives_it_a_fresh_countdown()
    {
        var archived = new Idea { Text = "x" }.WithStatus(IdeaStatus.Done, Now).Archived(Now.AddDays(14));

        var restored = archived.Restored(Now.AddDays(20));

        Assert.False(restored.IsArchived);
        Assert.Equal(IdeaStatus.Done, restored.Status);
        Assert.False(restored.ShouldAutoArchive(Now.AddDays(21)));
    }

    [Fact]
    public void Archiving_twice_keeps_the_first_date()
    {
        var archived = new Idea { Text = "x" }.Archived(Now).Archived(Now.AddDays(3));

        Assert.Equal(Now, archived.ArchivedAt);
    }

    [Fact]
    public void Clone_keeps_the_dates()
    {
        var idea = new Idea { Text = "x", DoneAt = Now, ArchivedAt = Now.AddDays(1) };

        var copy = idea.Clone();

        Assert.Equal(idea.DoneAt, copy.DoneAt);
        Assert.Equal(idea.ArchivedAt, copy.ArchivedAt);
    }
}
