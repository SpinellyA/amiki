using System.Net;
using Amiki.Application.Assistant;
using Amiki.Infrastructure.Assistant;
using Amiki.Api.Users;
using Amiki.Application.People;
using Amiki.Modules.Assistant;
using Amiki.Contracts.People;
using Microsoft.Extensions.Options;

namespace Amiki.Tests.People;

public class PeopleRulesTests
{
    private static readonly AppUser Owner = AppUser.CreateOwner("owner@gmail.com", new DateTime(2026, 10, 1));
    private static readonly AppUser Member = AppUser.Invite("friend@gmail.com", new DateTime(2026, 10, 1));

    private sealed class KeyCatcher : HttpMessageHandler
    {
        public string? Key { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Key = request.Headers.GetValues("x-goog-api-key").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"candidates":[{"content":{"parts":[{"text":"hi"}]}}]}"""),
            });
        }
    }

    private static (GeminiModel Model, KeyCatcher Http) Gemini(AppUser who, string? ownKey, string? serverKey = "server-key")
    {
        var user = new UserContext();
        user.SignIn(who, ownKey);
        var http = new KeyCatcher();
        return (new GeminiModel(new HttpClient(http), Options.Create(new GeminiOptions { ApiKey = serverKey }), user), http);
    }

    [Fact]
    public async Task The_owner_uses_the_servers_key_when_they_have_none_of_their_own()
    {
        var (model, http) = Gemini(Owner, ownKey: null);
        await model.WriteAsync("rules", "facts", CancellationToken.None);
        Assert.Equal("server-key", http.Key);
    }

    [Fact]
    public async Task Someone_else_never_gets_the_servers_key()
    {
        var (model, http) = Gemini(Member, ownKey: null);
        Assert.False(model.IsEnabled);
        await Assert.ThrowsAsync<LanguageModelException>(() => model.WriteAsync("rules", "facts", CancellationToken.None));
        Assert.Null(http.Key); // nothing was sent at all
    }

    [Fact]
    public async Task Everyone_uses_their_own_key_when_they_have_one()
    {
        var (model, http) = Gemini(Member, ownKey: "friends-key");
        await model.WriteAsync("rules", "facts", CancellationToken.None);
        Assert.Equal("friends-key", http.Key);
    }

    [Fact]
    public void Viewing_someone_switches_whose_data_it_is()
    {
        var user = new UserContext();
        user.SignIn(Owner, null);
        Assert.Equal(AppUser.OwnerId, user.DataUserId);

        user.View(Member.Id);
        Assert.True(user.IsViewingSomeoneElse);
        Assert.Equal(Member.Id, user.DataUserId);
        Assert.True(user.IsOwner); // still the owner signed in

        user.View(AppUser.OwnerId); // "viewing" yourself is just your own Amiki
        Assert.False(user.IsViewingSomeoneElse);
    }

    [Fact]
    public void A_members_save_keeps_what_the_owner_wrote()
    {
        var stored = new AssistantSettings { AboutMe = "Owner's words", Personality = "Kind", SeeMoney = true };
        var incoming = new AssistantSettings { AboutMe = "I changed it", Personality = "Pirate", SeeMoney = false };

        AssistantSettings.KeepOwnersWords(stored, incoming);

        Assert.Equal("Owner's words", incoming.AboutMe);
        Assert.Equal("Kind", incoming.Personality);
        Assert.False(incoming.SeeMoney); // the switches are theirs
    }

    [Fact]
    public void A_members_first_save_starts_with_nothing_written_for_her()
    {
        var incoming = new AssistantSettings { AboutMe = "sneaky", Personality = "pirate" };
        AssistantSettings.KeepOwnersWords(null, incoming);
        Assert.Equal(("", ""), (incoming.AboutMe, incoming.Personality));
    }

    [Fact]
    public void Remember_this_is_only_offered_to_the_owner()
    {
        var proposals = new AssistantProposals(null!);
        Assert.Contains(proposals.For(new AssistantSettings()), t => t.Name == "propose_remember");
        Assert.DoesNotContain(proposals.For(new AssistantSettings(), canRemember: false), t => t.Name == "propose_remember");
    }

    [Theory]
    [InlineData("friend@gmail.com", null)]
    [InlineData("  friend@gmail.com ", null)]
    [InlineData("", "Type their Google address.")]
    [InlineData("friend", "That doesn't look like an email address.")]
    [InlineData("a@b@c", "That doesn't look like an email address.")]
    [InlineData("@gmail.com", "That doesn't look like an email address.")]
    [InlineData("my friend@gmail.com", "That doesn't look like an email address.")]
    public void Adding_someone_needs_a_real_looking_address(string email, string? problem) =>
        Assert.Equal(problem, new AddPersonRequest(email).Validate());
}
