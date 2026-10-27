using Amiki.Application.Data;

namespace Amiki.Api.Endpoints;

/// <summary>
/// GET/HEAD /health for uptime monitors (UptimeRobot) and Render's health check: proves the app and
/// the database both answer, which also keeps a free-tier service from spinning down. Public on
/// purpose, and says nothing about anyone's data. HEAD is what uptime monitors send by default.
/// </summary>
public static class HealthEndpoints
{
    public static void MapHealth(this WebApplication app) =>
        app.MapMethods("/health", ["GET", "HEAD"], async (HttpContext http, IDatabaseHealth database, CancellationToken ct) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            return await database.AnswersAsync(ct)
                ? Results.Ok(new { status = "ok" })
                : Results.Json(new { status = "database unreachable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }).AllowAnonymous();
}
