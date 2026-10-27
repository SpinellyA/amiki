using Amiki.Api.Endpoints;
using Amiki.Api.Security;
using Amiki.Api.Users;
using Amiki.Application;
using Amiki.Application.People;
using Amiki.Domain;
using Amiki.Infrastructure;
using Amiki.Infrastructure.Data;

// The composition root: wires the layers together and maps each feature's endpoints. No rules here.

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Amiki")
    ?? throw new InvalidOperationException("Set the connection string ConnectionStrings:Amiki (environment variable ConnectionStrings__Amiki).");

// Who each request is for (see UserContextMiddleware); the database uses it to keep people's data apart.
builder.Services.AddScoped<UserContext>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<UserContext>());
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, connectionString);
builder.Services.ConfigureHttpJsonOptions(options => AmikiJson.Configure(options.SerializerOptions));
builder.Services.AddProblemDetails();
var auth = builder.AddGoogleSignIn();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await DatabaseStartup.MigrateAsync(scope.ServiceProvider);
    await scope.ServiceProvider.GetRequiredService<PeopleService>().EnsureOwnerAsync(auth.OwnerEmail, CancellationToken.None);
    if (app.Environment.IsDevelopment()) await scope.ServiceProvider.GetRequiredService<DevSeed>().RunAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    // Render (or any host) terminates HTTPS in front of the app; ASPNETCORE_FORWARDEDHEADERS_ENABLED
    // (set in the Dockerfile) makes requests look like the HTTPS they really are.
    app.UseExceptionHandler();
    app.UseHsts();
}

// The API also serves the Blazor app: one origin, one deploy, no CORS. MapStaticAssets (below)
// serves the app's files, _framework included, from the build's asset map.
app.UseRouting();
if (auth.Enabled) app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UserContextMiddleware>();
app.MapStaticAssets();

app.MapAuthEndpoints(auth);
app.MapHealth();

var api = app.MapGroup("/api");
if (auth.Enabled) api.RequireAuthorization(GoogleSignIn.Policy);
api.MapPlanning();
api.MapFinance();
api.MapReflection();
api.MapAssistant();
api.MapPeople();

// Unknown /api paths are real 404s; everything else is a client-side route.
app.MapFallback("/api/{**rest}", () => Results.NotFound());
// index.html must never be cached: after a deploy, a stale copy would point at files that no longer exist.
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});

app.Run();

// Lets the integration tests start this app (WebApplicationFactory<Program>).
public partial class Program;
