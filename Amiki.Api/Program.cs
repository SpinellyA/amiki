using Amiki.Api.Data;
using Amiki.Api.Endpoints;
using Amiki.Api.Security;
using Amiki.Core;
using Amiki.Modules.Finance;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Amiki")
    ?? throw new InvalidOperationException("Set the connection string ConnectionStrings:Amiki (environment variable ConnectionStrings__Amiki).");

builder.Services.AddDbContext<AmikiDb>(options => options.UseNpgsql(connectionString));
builder.Services.ConfigureHttpJsonOptions(options => AmikiJson.Configure(options.SerializerOptions));
builder.Services.AddProblemDetails();
var auth = builder.AddOwnerAuth();

var app = builder.Build();

// One person, one instance: applying migrations at startup keeps deploys to a single step.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AmikiDb>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment()) await DevSeed.RunAsync(db);
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
app.MapStaticAssets();

app.MapAuthEndpoints(auth);

var api = app.MapGroup("/api");
if (auth.Enabled) api.RequireAuthorization(OwnerAuth.Policy);

api.MapEntity("inbox", db => db.Inbox);
api.MapEntity("tasks", db => db.Tasks);
api.MapEntity("ideas", db => db.Ideas);
api.MapEntity("transactions", db => db.Transactions);
api.MapEntity("plans", db => db.Plans, Plan.CheckReplace);
api.MapGet("/accounts", async (AmikiDb db, CancellationToken ct) =>
    Results.Ok(await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct)));

// Unknown /api paths are real 404s; everything else is a client-side route.
app.MapFallback("/api/{**rest}", () => Results.NotFound());
// index.html must never be cached: after a deploy, a stale copy would point at files that no longer exist.
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});

app.Run();
