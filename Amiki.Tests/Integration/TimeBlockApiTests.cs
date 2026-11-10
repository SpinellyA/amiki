using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amiki.Tests.Integration;

[Collection(AmikiAppCollection.Name)]
public class TimeBlockApiTests(AmikiApp app)
{
    private static readonly JsonSerializerOptions Json = AmikiApp.Json;
    private static readonly DateTime Day = new(2026, 11, 11);

    private static TimeBlock Block(int fromHour, int toHour, string label) =>
        new() { Day = Day, StartMinute = fromHour * 60, EndMinute = toHour * 60, Label = label };

    [Fact]
    public async Task A_block_saves_and_comes_back_only_for_its_person()
    {
        var (_, friend) = await app.AddPersonAsync();
        var block = Block(14, 16, "Lab report");

        (await app.As(friend).PutAsJsonAsync($"api/time-blocks/{block.Id}", block, Json)).EnsureSuccessStatusCode();

        var saved = Assert.Single((await app.As(friend).GetFromJsonAsync<List<TimeBlock>>("api/time-blocks", Json))!);
        Assert.Equal((Day, 14 * 60, 16 * 60, "Lab report"), (saved.Day, saved.StartMinute, saved.EndMinute, saved.Label));
        Assert.DoesNotContain((await app.As().GetFromJsonAsync<List<TimeBlock>>("api/time-blocks", Json))!, b => b.Id == block.Id);
    }

    [Fact]
    public async Task A_block_that_overlaps_another_is_refused_but_moving_one_is_fine()
    {
        var (_, friend) = await app.AddPersonAsync();
        var gym = Block(9, 10, "Gym");
        (await app.As(friend).PutAsJsonAsync($"api/time-blocks/{gym.Id}", gym, Json)).EnsureSuccessStatusCode();

        var clash = Block(9, 11, "Read");
        var refused = await app.As(friend).PutAsJsonAsync($"api/time-blocks/{clash.Id}", clash, Json);
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Contains("Gym", await refused.Content.ReadAsStringAsync());

        gym.EndMinute = 10 * 60 + 30;   // its own old time never counts against it
        (await app.As(friend).PutAsJsonAsync($"api/time-blocks/{gym.Id}", gym, Json)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Someone_elses_block_doesnt_clash_with_yours()
    {
        var (_, one) = await app.AddPersonAsync();
        var (_, two) = await app.AddPersonAsync();
        var mine = Block(18, 19, "Dinner");
        var theirs = Block(18, 19, "Dinner");

        (await app.As(one).PutAsJsonAsync($"api/time-blocks/{mine.Id}", mine, Json)).EnsureSuccessStatusCode();
        (await app.As(two).PutAsJsonAsync($"api/time-blocks/{theirs.Id}", theirs, Json)).EnsureSuccessStatusCode();
    }
}
