using System.Security.Claims;
using Amiki.Api.Data;
using Amiki.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Users;

/// <summary>
/// Works out who each /api and /auth request is for, before anything touches the database:
///   - the signed-in Google account must be an enabled person in app_users (else 401);
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

    public async Task InvokeAsync(HttpContext http, UserContext user, AmikiDb db, OwnerAuth.Settings auth, ApiKeyProtector keys)
    {
        var path = http.Request.Path;
        if (!path.StartsWithSegments("/api") && !path.StartsWithSegments("/auth/me"))
        {
            await next(http);
            return;
        }

        var person = await WhoAsync(http, db, auth);
        if (person is null)
        {
            // Not signed in, or signed in with an account that isn't (or is no longer) allowed.
            if (path.StartsWithSegments("/api") || auth.Enabled)
            {
                http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await next(http);
            return;
        }
        user.SignIn(person, keys.Unprotect(person.GeminiKeyProtected));

        if (http.Request.Headers.TryGetValue(ViewAsHeader, out var viewAs) && !string.IsNullOrWhiteSpace(viewAs))
        {
            if (!user.IsOwner || !Guid.TryParse(viewAs, out var targetId) || !await db.Users.AnyAsync(u => u.Id == targetId))
            {
                await Refuse(http, StatusCodes.Status403Forbidden, "You can't view that Amiki.");
                return;
            }
            user.View(targetId);
            if (user.IsViewingSomeoneElse && !HttpMethods.IsGet(http.Request.Method) && !AllowedWhileViewing.Any(p => path.StartsWithSegments(p)))
            {
                await Refuse(http, StatusCodes.Status403Forbidden, "Viewing someone else's Amiki is read-only.");
                return;
            }
        }

        await next(http);
    }

    private static async Task<AppUser?> WhoAsync(HttpContext http, AmikiDb db, OwnerAuth.Settings auth)
    {
        if (!auth.Enabled)
        {
            // Local development without sign-in: you're the owner, or whoever the dev header names.
            var devEmail = http.Request.Headers[DevUserHeader].ToString().Trim().ToLowerInvariant();
            return devEmail.Length > 0
                ? await db.Users.FirstOrDefaultAsync(u => u.Email == devEmail && u.Enabled)
                : await db.Users.FirstOrDefaultAsync(u => u.Id == AppUser.OwnerId);
        }

        if (http.User.Identity?.IsAuthenticated != true) return null;
        var email = http.User.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
        return email is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Enabled);
    }

    private static Task Refuse(HttpContext http, int status, string detail)
    {
        http.Response.StatusCode = status;
        return http.Response.WriteAsJsonAsync(new { title = "Not allowed", status, detail });
    }
}
