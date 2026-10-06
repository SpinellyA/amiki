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

// For uptime monitors (UptimeRobot) and Render's health check: proves the app and the database
// both answer, which also keeps a free-tier service from spinning down. Public on purpose, and
// says nothing about your data. HEAD is included because that's what uptime monitors send by default.
// Its own unpooled connection with hard limits, so a database that accepts the connection but
// never answers still fails fast (503) instead of hanging the monitor. 8s leaves room for Neon
// waking a suspended database.
var healthConnection = new Npgsql.NpgsqlConnectionStringBuilder(connectionString)
{
    Timeout = 8,
    CommandTimeout = 8,
    CancellationTimeout = 1000,
    Pooling = false,
}.ConnectionString;

app.MapMethods("/health", ["GET", "HEAD"], async (HttpContext http) =>
{
    http.Response.Headers.CacheControl = "no-store";
    try
    {
        await using var connection = new Npgsql.NpgsqlConnection(healthConnection);
        await connection.OpenAsync();
        await using var ping = new Npgsql.NpgsqlCommand("SELECT 1", connection);
        await ping.ExecuteScalarAsync();
        return Results.Ok(new { status = "ok" });
    }
    catch (Exception)
    {
        return Results.Json(new { status = "database unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}).AllowAnonymous();

var api = app.MapGroup("/api");
if (auth.Enabled) api.RequireAuthorization(OwnerAuth.Policy);

api.MapEntity("inbox", db => db.Inbox);
api.MapEntity("tasks", db => db.Tasks);
api.MapEntity("ideas", db => db.Ideas);
api.MapEntity("plans", db => db.Plans, Plan.CheckReplace);

// Money: every change is audited, and account and category names must be real ones.
api.MapEntity("categories", db => db.Categories,
    checkReplace: Category.CheckReplace,
    checkAsync: MoneyRules.CategoryNameIsFree,
    describe: c => c.Describe(),
    checkDelete: MoneyRules.CategoryCanBeDeleted);
api.MapEntity("transactions", db => db.Transactions,
    checkAsync: async (db, tx, ct) => await MoneyRules.AccountExists(db, tx.Account, ct) ?? await MoneyRules.CategoryExists(db, tx, ct),
    describe: tx => tx.Describe());
api.MapEntity("transfers", db => db.Transfers,
    checkAsync: async (db, t, ct) => await MoneyRules.AccountExists(db, t.From, ct) ?? await MoneyRules.AccountExists(db, t.To, ct),
    describe: t => t.Describe());
api.MapEntity("balance-checks", db => db.BalanceChecks,
    checkReplace: MoneyRules.ChecksAreFinal,
    checkAsync: MoneyRules.CheckIsBacked,
    describe: c => c.Describe());
api.MapGet("/accounts", async (AmikiDb db, CancellationToken ct) =>
    Results.Ok(await db.Accounts.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct)));
// Read-only on purpose: the log is only ever written alongside the change it records.
api.MapGet("/audit", async (AmikiDb db, CancellationToken ct) =>
    Results.Ok(await db.AuditLog.AsNoTracking().OrderByDescending(e => e.Id).Take(500).ToListAsync(ct)));

// Unknown /api paths are real 404s; everything else is a client-side route.
app.MapFallback("/api/{**rest}", () => Results.NotFound());
// index.html must never be cached: after a deploy, a stale copy would point at files that no longer exist.
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
});

app.Run();
