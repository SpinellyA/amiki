using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Amiki.Modules.Diary;
using Amiki.Modules.Tasks;

namespace Amiki.Tests.Integration;

/// <summary>The promises made to everyone the owner adds: their data is theirs, the diary is private.</summary>
[Collection(AmikiAppCollection.Name)]
public class PeopleIsolationTests(AmikiApp app)
{
    private static readonly JsonSerializerOptions Json = AmikiApp.Json;

    [Fact]
    public async Task Each_person_only_sees_their_own_things()
    {
        var (_, friend) = await app.AddPersonAsync();
        var task = new TaskItem { Title = "Friend's task" };

        (await app.As(friend).PutAsJsonAsync($"api/tasks/{task.Id}", task, Json)).EnsureSuccessStatusCode();

        Assert.Contains(await TasksAsync(app.As(friend)), t => t.Id == task.Id);
        Assert.DoesNotContain(await TasksAsync(app.As()), t => t.Id == task.Id);
    }

    [Fact]
    public async Task Someone_else_cant_change_or_delete_your_things()
    {
        var (_, friend) = await app.AddPersonAsync();
        var task = new TaskItem { Title = "Mine" };
        (await app.As(friend).PutAsJsonAsync($"api/tasks/{task.Id}", task, Json)).EnsureSuccessStatusCode();

        // The owner using the same id writes their own row, and deleting it only touches theirs.
        (await app.As().PutAsJsonAsync($"api/tasks/{task.Id}", new TaskItem { Id = task.Id, Title = "Owner's" }, Json)).EnsureSuccessStatusCode();
        (await app.As().DeleteAsync($"api/tasks/{task.Id}")).EnsureSuccessStatusCode();

        var theirs = Assert.Single(await TasksAsync(app.As(friend)), t => t.Id == task.Id);
        Assert.Equal("Mine", theirs.Title);
    }

    [Fact]
    public async Task Everyone_can_have_their_own_diary_page_for_the_same_day()
    {
        var (_, friend) = await app.AddPersonAsync();
        var day = new DateTime(2026, 3, 14);
        var mine = DiaryEntry.For(day);
        mine.Text = "owner's page";
        var theirs = DiaryEntry.For(day);
        theirs.Text = "friend's page";

        (await app.As().PutAsJsonAsync($"api/diary/{mine.Id}", mine, Json)).EnsureSuccessStatusCode();
        (await app.As(friend).PutAsJsonAsync($"api/diary/{theirs.Id}", theirs, Json)).EnsureSuccessStatusCode();

        Assert.Equal("friend's page", (await DiaryAsync(app.As(friend))).Single(e => e.Id == theirs.Id).Text);
        Assert.Equal("owner's page", (await DiaryAsync(app.As())).Single(e => e.Id == mine.Id).Text);
        await app.As().DeleteAsync($"api/diary/{mine.Id}");
    }

    [Fact]
    public async Task New_people_start_with_their_own_accounts_and_categories()
    {
        var (_, friend) = await app.AddPersonAsync();

        var accounts = await app.As(friend).GetFromJsonAsync<List<JsonElement>>("api/accounts", Json);
        var categories = await app.As(friend).GetFromJsonAsync<List<JsonElement>>("api/categories", Json);

        Assert.Equal(["Cash", "GCash", "Landbank", "Maribank"], accounts!.Select(a => a.GetProperty("name").GetString()));
        Assert.NotEmpty(categories!);
    }

    [Fact]
    public async Task The_owner_can_look_but_not_touch_and_never_read_the_diary()
    {
        var (id, friend) = await app.AddPersonAsync();
        var task = new TaskItem { Title = "Visible to the owner" };
        (await app.As(friend).PutAsJsonAsync($"api/tasks/{task.Id}", task, Json)).EnsureSuccessStatusCode();
        var page = DiaryEntry.For(new DateTime(2026, 3, 15));
        page.Text = "secret";
        (await app.As(friend).PutAsJsonAsync($"api/diary/{page.Id}", page, Json)).EnsureSuccessStatusCode();

        var viewing = app.As(viewing: id);

        Assert.Contains(await TasksAsync(viewing), t => t.Id == task.Id);
        Assert.Empty(await DiaryAsync(viewing));
        var write = await viewing.PutAsJsonAsync($"api/tasks/{Guid.NewGuid()}", new TaskItem { Title = "sneaky" }, Json);
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task People_other_than_the_owner_cant_manage_or_view_anyone()
    {
        var (_, friend) = await app.AddPersonAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await app.As(friend).GetAsync("api/people")).StatusCode);
        var peeking = app.As(friend, viewing: Guid.Parse("00000000-0000-4000-8000-0000000000a1"));
        Assert.Equal(HttpStatusCode.Forbidden, (await peeking.GetAsync("api/tasks")).StatusCode);
    }

    [Fact]
    public async Task Someone_who_was_never_added_gets_nothing()
    {
        var response = await app.As("stranger@gmail.com").GetAsync("api/tasks");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Only_the_owner_writes_what_Amiki_knows_and_how_she_talks()
    {
        var (id, friend) = await app.AddPersonAsync();
        (await app.As().PutAsJsonAsync($"api/people/{id}/assistant", new { aboutMe = "Owner wrote this", personality = "Kind" }, Json))
            .EnsureSuccessStatusCode();

        var settingsId = "a5515700-0000-4000-8000-000000000001";
        var attempt = new { id = settingsId, aboutMe = "I changed it", personality = "Pirate", seeMoney = false, thinking = "low" };
        (await app.As(friend).PutAsJsonAsync($"api/assistant-settings/{settingsId}", attempt, Json)).EnsureSuccessStatusCode();

        var saved = Assert.Single((await app.As(friend).GetFromJsonAsync<List<JsonElement>>("api/assistant-settings", Json))!);
        Assert.Equal("Owner wrote this", saved.GetProperty("aboutMe").GetString());
        Assert.Equal("Kind", saved.GetProperty("personality").GetString());
        Assert.False(saved.GetProperty("seeMoney").GetBoolean()); // their own switches still save
    }

    [Fact]
    public async Task Removing_someone_removes_everything_of_theirs()
    {
        var (id, friend) = await app.AddPersonAsync();
        var task = new TaskItem { Title = "Gone soon" };
        (await app.As(friend).PutAsJsonAsync($"api/tasks/{task.Id}", task, Json)).EnsureSuccessStatusCode();

        (await app.As().DeleteAsync($"api/people/{id}")).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await app.As(friend).GetAsync("api/tasks")).StatusCode);
        // Added again, they start fresh: nothing of before comes back.
        var again = await app.As().PostAsJsonAsync("api/people", new { email = friend }, Json);
        again.EnsureSuccessStatusCode();
        Assert.DoesNotContain(await TasksAsync(app.As(friend)), t => t.Id == task.Id);
    }

    private static async Task<List<TaskItem>> TasksAsync(HttpClient client) =>
        await client.GetFromJsonAsync<List<TaskItem>>("api/tasks", Json) ?? [];

    private static async Task<List<DiaryEntry>> DiaryAsync(HttpClient client) =>
        await client.GetFromJsonAsync<List<DiaryEntry>>("api/diary", Json) ?? [];
}
