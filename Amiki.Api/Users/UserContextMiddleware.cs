using System.Security.Claims;
using Amiki.Api.Security;
using Amiki.Application.People;
using Amiki.Domain.People;

namespace Amiki.Api.Users;

/// <summary>
/// Works out who each /api and /auth/me request is for, before anything touches the data:
///   - the signed-in Google account must be an enabled person (else 401);
///   - the owner may send "X-Amiki-View-As: {user id}" to look at someone's Amiki, which is
///     read-only: anything but reading is refused (403).
/// In Development without Google sign-in, everyone is the owner, unless "X-Dev-User: {email}"
/// picks another person (for testing; impossible once sign-in is configured).
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    public const string ViewAsHeader = "X-Amiki-View-As";
    public const string DevUserHeader = "X-Dev-User";

    // Viewing is look-only. The dashboard card is a POST but only reads when viewing (it never writes a note then).
    private static readonly string[] AllowedWhileViewing = ["/api/assistant/briefing"];

    public async Task InvokeAsync(HttpContext http, UserContext user, SignInService signIn, GoogleSignIn.Settings auth, ISecretProtector secrets)
    {
        var path = http.Request.Path;
        if (!path.StartsWithSegments("/api") && !path.StartsWithSegments("/auth/me"))
        {
            await next(http);
            return;
        }

        if (await WhoAsync(http, signIn, auth) is not { } person)
        {
            // Not signed in, or signed in with an account that isn't (or is no longer) allowed.
            http.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        user.SignIn(person, secrets.Unprotect(person.GeminiKeyProtected));

        if (http.Request.Headers.TryGetValue(ViewAsHeader, out var viewAs) && !string.IsNullOrWhiteSpace(viewAs))
        {
            if (!user.IsOwner || !Guid.TryParse(viewAs, out var targetId) || !await signIn.ExistsAsync(targetId, http.RequestAborted))
            {
                await RefuseAsync(http, "You can't view that Amiki.");
                return;
            }
            user.View(targetId);
            if (user.IsViewingSomeoneElse && !HttpMethods.IsGet(http.Request.Method) && !AllowedWhileViewing.Any(p => path.StartsWithSegments(p)))
            {
                await RefuseAsync(http, "Viewing someone else's Amiki is read-only.");
                return;
            }
        }

        await next(http);
    }

    private static Task<AppUser?> WhoAsync(HttpContext http, SignInService signIn, GoogleSignIn.Settings auth)
    {
        if (!auth.Enabled)
        {
            // Local development without sign-in: you're the owner, or whoever the dev header names.
            var devEmail = http.Request.Headers[DevUserHeader].ToString();
            return devEmail.Length > 0
                ? signIn.FindAllowedAsync(devEmail, http.RequestAborted)
                : signIn.FindAllowedAsync(AppUser.OwnerId, http.RequestAborted);
        }

        return http.User.Identity?.IsAuthenticated == true
            ? signIn.FindAllowedAsync(http.User.FindFirstValue(ClaimTypes.Email), http.RequestAborted)
            : Task.FromResult<AppUser?>(null);
    }

    private static Task RefuseAsync(HttpContext http, string detail)
    {
        http.Response.StatusCode = StatusCodes.Status403Forbidden;
        return http.Response.WriteAsJsonAsync(new { title = "Not allowed", status = StatusCodes.Status403Forbidden, detail });
    }
}
