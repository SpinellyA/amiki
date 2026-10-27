using System.Net.Http.Json;
using System.Text.Json;
using Amiki.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;

namespace Amiki.Tests.Integration;

/// <summary>
/// The real Amiki API on a throwaway Postgres (Docker), shared by every integration test.
/// Runs in Development without Google sign-in, so a request is the owner unless it names
/// someone with the X-Dev-User header, exactly like local development.
/// </summary>
public sealed class AmikiApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    public static readonly JsonSerializerOptions Json = AmikiJson.Create();

    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder().WithImage("postgres:17-alpine").Build();

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _db.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Amiki", _db.GetConnectionString());
        // Never reach a real model from tests, whatever the developer's local settings say.
        builder.UseSetting("Gemini:ApiKey", "");
    }

    /// <summary>A client for the owner, or for the person with <paramref name="email"/>.</summary>
    public HttpClient As(string? email = null, Guid? viewing = null)
    {
        var client = CreateClient();
        if (email is not null) client.DefaultRequestHeaders.Add("X-Dev-User", email);
        if (viewing is not null) client.DefaultRequestHeaders.Add("X-Amiki-View-As", viewing.ToString());
        return client;
    }

    /// <summary>Adds someone new (as the owner) and returns their id.</summary>
    public async Task<(Guid Id, string Email)> AddPersonAsync()
    {
        var email = $"friend-{Guid.NewGuid():N}@gmail.com";
        var response = await As().PostAsJsonAsync("api/people", new { email }, Json);
        response.EnsureSuccessStatusCode();
        var person = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return (person.GetProperty("id").GetGuid(), email);
    }
}

[CollectionDefinition(Name)]
public sealed class AmikiAppCollection : ICollectionFixture<AmikiApp>
{
    public const string Name = "Amiki API";
}
