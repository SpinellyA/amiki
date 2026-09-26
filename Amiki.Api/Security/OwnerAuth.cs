using System.Security.Claims;
using Amiki.Api.Data;
using Amiki.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;

namespace Amiki.Api.Security;

/// <summary>
/// Sign-in with Google, open to the addresses in Amiki:AllowedEmails only.
///
/// The server runs the whole OAuth exchange and then keeps you signed in with an HttpOnly cookie,
/// so no token ever lives in browser JavaScript. Other Google accounts are turned away at the
/// callback (no cookie is issued) and, as a second check, every /api request re-checks the email.
///
/// In Development without Google credentials configured, sign-in is off so local work needs no
/// setup. Anywhere else the app refuses to start unless Google and the allowlist are configured.
/// </summary>
public static class OwnerAuth
{
    public const string Policy = "Owner";

    public sealed record Settings(bool Enabled, IReadOnlySet<string> AllowedEmails);

    public static Settings AddOwnerAuth(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var clientId = config["Authentication:Google:ClientId"];
        var clientSecret = config["Authentication:Google:ClientSecret"];
        var allowed = config.GetSection("Amiki:AllowedEmails").Get<string[]>()?
            .Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var googleConfigured = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
        if (!googleConfigured && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException(
                "Google sign-in isn't configured. Set Authentication__Google__ClientId and Authentication__Google__ClientSecret.");
        if (googleConfigured && allowed.Count == 0)
            throw new InvalidOperationException("Set Amiki:AllowedEmails, or nobody will be able to sign in.");

        // Cookie encryption keys live in Postgres, so restarts and redeploys don't sign you out.
        builder.Services.AddDataProtection().SetApplicationName("Amiki").PersistKeysToDbContext<AmikiDb>();

        var settings = new Settings(googleConfigured, allowed);
        builder.Services.AddSingleton(settings);
        if (!googleConfigured)
        {
            builder.Services.AddAuthorization();
            return settings;
        }

        builder.Services
            // The cookie is the default for everything, including challenges, so an API call without
            // a session gets a plain 401. Only /auth/login sends the browser to Google, explicitly.
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "amiki.auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                // Long and sliding: signing in on your phone once should last the semester.
                options.ExpireTimeSpan = TimeSpan.FromDays(60);
                options.SlidingExpiration = true;
                // It's an API: answer 401/403 instead of redirecting fetch calls to a login page.
                options.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
                options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
            })
            .AddGoogle(options =>
            {
                options.ClientId = clientId!;
                options.ClientSecret = clientSecret!;
                options.ClaimActions.MapJsonKey("email_verified", "email_verified");
                options.Events.OnTicketReceived = ctx =>
                {
                    if (!IsOwner(ctx.Principal, allowed))
                    {
                        ctx.Response.Redirect("/?auth=denied");
                        ctx.HandleResponse(); // stops here: no cookie for this account
                    }
                    return Task.CompletedTask;
                };
                options.Events.OnRemoteFailure = ctx =>
                {
                    ctx.Response.Redirect("/?auth=failed");
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.RequireAuthenticatedUser().RequireAssertion(ctx => IsOwner(ctx.User, allowed)));

        return settings;
    }

    public static void MapAuthEndpoints(this WebApplication app, Settings settings)
    {
        var auth = app.MapGroup("/auth");

        auth.MapGet("/me", (ClaimsPrincipal user) =>
        {
            if (!settings.Enabled) return Results.Ok(new SessionInfo("dev@localhost", "Local dev", SignInEnabled: false));
            return IsOwner(user, settings.AllowedEmails)
                ? Results.Ok(new SessionInfo(user.FindFirstValue(ClaimTypes.Email)!, user.FindFirstValue(ClaimTypes.Name) ?? "", SignInEnabled: true))
                : Results.Unauthorized();
        });

        if (!settings.Enabled) return;

        auth.MapGet("/login", (string? returnUrl) =>
            Results.Challenge(new AuthenticationProperties { RedirectUri = LocalOnly(returnUrl) }, [GoogleDefaults.AuthenticationScheme]));

        // POST so another site can't sign you out with a link or an image tag.
        auth.MapPost("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });
    }

    private static bool IsOwner(ClaimsPrincipal? user, IReadOnlySet<string> allowed)
    {
        var email = user?.FindFirstValue(ClaimTypes.Email);
        var verified = user?.FindFirstValue("email_verified");
        return email is not null
            && allowed.Contains(email)
            // Refuse an address Google itself marks as unverified (a Gmail address always is verified).
            && (verified is null || verified.Equals("true", StringComparison.OrdinalIgnoreCase));
    }

    // Only redirect back into Amiki itself, never to another site ("//evil.com" included).
    private static string LocalOnly(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";
}
