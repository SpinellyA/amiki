using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amiki.Tests.Integration;

[Collection(AmikiAppCollection.Name)]
public class WishlistTests(AmikiApp app)
{
    private static readonly JsonSerializerOptions Json = AmikiApp.Json;

    [Fact]
    public async Task Your_wishlist_is_saved_and_only_yours()
    {
        var (_, friend) = await app.AddPersonAsync();
        var tablet = new WishItem { Name = "Drawing tablet", Price = 4500, Status = WishStatus.Saving, Link = "https://shopee.ph/tablet" };

        (await app.As(friend).PutAsJsonAsync($"api/wishlist/{tablet.Id}", tablet, Json)).EnsureSuccessStatusCode();

        var theirs = Assert.Single((await app.As(friend).GetFromJsonAsync<List<WishItem>>("api/wishlist", Json))!);
        Assert.Equal(("Drawing tablet", 4500m, WishStatus.Saving), (theirs.Name, theirs.Price, theirs.Status));
        Assert.DoesNotContain((await app.As().GetFromJsonAsync<List<WishItem>>("api/wishlist", Json))!, w => w.Id == tablet.Id);
    }

    [Fact]
    public async Task Wishlist_money_is_saved_per_person()
    {
        var (_, friend) = await app.AddPersonAsync();
        var fund = new WishlistFund().SetTo(3000, DateTime.Now);

        (await app.As(friend).PutAsJsonAsync($"api/wishlist-fund/{fund.Id}", fund, Json)).EnsureSuccessStatusCode();

        Assert.Equal(3000, Assert.Single((await app.As(friend).GetFromJsonAsync<List<WishlistFund>>("api/wishlist-fund", Json))!).Amount);
        Assert.Empty((await app.As().GetFromJsonAsync<List<WishlistFund>>("api/wishlist-fund", Json))!);
    }

    [Fact]
    public async Task A_bad_item_is_refused()
    {
        var bad = new WishItem { Name = "Tablet", Price = 100, Link = "javascript:alert(1)" };

        var response = await app.As().PutAsJsonAsync($"api/wishlist/{bad.Id}", bad, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
