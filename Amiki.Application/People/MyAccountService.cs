using Amiki.Application.Common;
using Amiki.Application.Data;
using Amiki.Contracts;
using Amiki.Contracts.People;
using Amiki.Domain.People;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.People;

/// <summary>What anyone can do about their own account: see who they are, their key, the notice.</summary>
public sealed class MyAccountService(IAmikiDb db, ICurrentUser me, ISecretProtector secrets, TimeProvider time)
{
    /// <param name="signInEnabled">False in local development without Google sign-in.</param>
    public async Task<SessionInfo> SessionAsync(bool signInEnabled, CancellationToken ct)
    {
        var person = await MeAsync(ct);
        var name = signInEnabled ? person.Name : person.IsOwner ? "Local dev" : person.Email;
        return new SessionInfo(person.Email, name, signInEnabled, person.IsOwner, NoticePending: person.NoticeSeenAt is null);
    }

    public async Task<MyKeyInfo> KeyAsync(CancellationToken ct) => new((await MeAsync(ct)).GeminiKeySetBy);

    /// <summary>Their own key replaces one the owner set. Empty removes it.</summary>
    public async Task<Outcome<MyKeyInfo>> SetKeyAsync(GeminiKeyRequest request, CancellationToken ct)
    {
        if (request.Validate() is { } problem) return Outcome.Invalid(problem);
        var person = await MeAsync(ct);
        person.SetGeminiKey(string.IsNullOrWhiteSpace(request.Key) ? null : secrets.Protect(request.Key), KeySource.Self);
        await db.SaveChangesAsync(ct);
        return new MyKeyInfo(person.GeminiKeySetBy);
    }

    public async Task NoticeSeenAsync(CancellationToken ct)
    {
        (await MeAsync(ct)).SawNotice(time.GetLocalNow().DateTime);
        await db.SaveChangesAsync(ct);
    }

    private Task<AppUser> MeAsync(CancellationToken ct) => db.Users.FirstAsync(u => u.Id == me.SignedInId, ct);
}
