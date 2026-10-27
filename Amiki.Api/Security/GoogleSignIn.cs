using System.Security.Claims;
using Amiki.Application.People;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace Amiki.Api.Security;

/// <summary>
/// Sign-in with Google, open to the owner (Amiki:OwnerEmail) and the people the owner has added.
///
/// The server runs the whole OAuth exchange and then keeps you signed in with an HttpOnly cookie,
/// so no token ever lives in browser JavaScript. Accounts that aren't on the list are turned away at
/// the callback (no cookie is issued) and, as a second check, every /api request looks the person
/// up again (<see cref="Users.UserContextMiddleware"/>), so removing someone locks them out at once.
///
/// In Development without Google credentials configured, sign-in is off so local work needs no
/// setup. Anywhere else the app refuses to start unless Google and the owner are configured.
/// </summary>
public static class GoogleSignIn
{
    public const string Policy = "SignedIn";

    public sealed record Settings(bool Enabled, string OwnerEmail);

    public static Settings AddGoogleSignIn(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        var clientId = config["Authentication:Google:ClientId"];
        var clientSecret = config["Authentication:Google:ClientSecret"];
        // Amiki:AllowedEmails is the older setting; its first address is the owner.
        var ownerEmail = config["Amiki:OwnerEmail"]
            ?? config.GetSection("Amiki:AllowedEmails").Get<string[]>()?.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

        var googleConfigured = !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret);
        if (!googleConfigured && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException(
                "Google sign-in isn't configured. Set Authentication__Google__ClientId and Authentication__Google__ClientSecret.");
        if (googleConfigured && string.IsNullOrWhiteSpace(ownerEmail))
            throw new InvalidOperationException("Set Amiki:OwnerEmail, or nobody will be able to sign in.");

        var settings = new Settings(googleConfigured, (ownerEmail ?? "owner@localhost").Trim().ToLowerInvariant());
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
                options.Events.OnTicketReceived = async ctx =>
                {
                    var principal = ctx.Principal!;
                    var verified = principal.FindFirstValue("email_verified") is not { } v || v.Equals("true", StringComparison.OrdinalIgnoreCase);
                    var signIn = ctx.HttpContext.RequestServices.GetRequiredService<SignInService>();
                    if (!await signIn.SignInAsync(principal.FindFirstValue(ClaimTypes.Email), verified, principal.FindFirstValue(ClaimTypes.Name), ctx.HttpContext.RequestAborted))
                    {
                        ctx.Response.Redirect("/?auth=denied");
                        ctx.HandleResponse(); // stops here: no cookie for this account
                    }
                };
                options.Events.OnRemoteFailure = ctx =>
                {
                    ctx.Response.Redirect("/?auth=failed");
                    ctx.HandleResponse();
                    return Task.CompletedTask;
                };
            });

        // Who exactly may use the API is checked against the database by UserContextMiddleware.
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policy, policy => policy.RequireAuthenticatedUser());

        return settings;
    }

    public static void MapAuthEndpoints(this WebApplication app, Settings settings)
    {
        var auth = app.MapGroup("/auth");

        // UserContextMiddleware has already refused anyone who isn't allowed in.
        auth.MapGet("/me", async (MyAccountService account, CancellationToken ct) => Results.Ok(await account.SessionAsync(settings.Enabled, ct)));

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

    // Only redirect back into Amiki itself, never to another site ("//evil.com" included).
    private static string LocalOnly(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")
            ? returnUrl
            : "/";
}
