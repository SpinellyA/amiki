using Amiki.Application.Common;
using Amiki.Application.Data;
using Amiki.Contracts.People;
using Amiki.Domain.People;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.People;

/// <summary>
/// The owner's People page: who can sign in, their keys, and what Amiki knows about them.
/// Viewing someone's Amiki isn't here; that's the X-Amiki-View-As header, read-only.
/// </summary>
public sealed class PeopleService(IAmikiDb db, IPeopleData people, ISecretProtector secrets, TimeProvider time)
{
    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct) =>
        (await db.Users.AsNoTracking().ToListAsync(ct))
            .OrderByDescending(u => u.IsOwner).ThenBy(u => u.AddedAt)
            .Select(ToPerson)
            .ToList();

    public async Task<Outcome<Person>> AddAsync(AddPersonRequest request, CancellationToken ct)
    {
        if (request.Validate() is { } problem) return Outcome.Invalid(problem);
        var email = AppUser.Normalize(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, ct)) return Outcome.Conflict($"{email} can already sign in.");

        var person = AppUser.Invite(email, Now);
        person.SetGeminiKey(Protect(request.GeminiKey), KeySource.Owner);
        db.Users.Add(person);
        await db.SaveChangesAsync(ct);
        await people.ProvideStarterDataAsync(person.Id, ct);
        return ToPerson(person);
    }

    public async Task<Outcome<Person>> SetEnabledAsync(Guid id, bool enabled, CancellationToken ct)
    {
        if (id == AppUser.OwnerId) return Outcome.Invalid("You can't turn yourself off.");
        if (await FindAsync(id, ct) is not { } person) return Outcome.NotFound();
        person.SetEnabled(enabled);
        await db.SaveChangesAsync(ct);
        return ToPerson(person);
    }

    public async Task<Outcome<Person>> SetKeyAsync(Guid id, GeminiKeyRequest request, CancellationToken ct)
    {
        if (request.Validate() is { } problem) return Outcome.Invalid(problem);
        if (await FindAsync(id, ct) is not { } person) return Outcome.NotFound();
        person.SetGeminiKey(Protect(request.Key), KeySource.Owner);
        await db.SaveChangesAsync(ct);
        return ToPerson(person);
    }

    public async Task<Outcome<AssistantWords>> WordsAsync(Guid id, CancellationToken ct)
    {
        if (await FindAsync(id, ct) is null) return Outcome.NotFound();
        var settings = await people.AssistantSettingsForAsync(id, ct);
        return new AssistantWords(settings.AboutMe, settings.Personality);
    }

    public async Task<Outcome> SetWordsAsync(Guid id, AssistantWords words, CancellationToken ct)
    {
        if (words.Validate() is { } problem) return Outcome.Invalid(problem);
        if (await FindAsync(id, ct) is null) return Outcome.NotFound();
        var settings = await people.AssistantSettingsForAsync(id, ct);
        settings.AboutMe = words.AboutMe.Trim();
        settings.Personality = words.Personality.Trim();
        settings.UpdatedAt = Now;
        await db.SaveChangesAsync(ct);
        return Outcome.Ok;
    }

    public async Task<Outcome> RemoveAsync(Guid id, CancellationToken ct)
    {
        if (id == AppUser.OwnerId) return Outcome.Invalid("You can't remove yourself.");
        await people.DeleteEverythingAsync(id, ct);
        return Outcome.Ok;
    }

    /// <summary>At startup: the owner from the config exists, with their starter accounts and categories.</summary>
    public async Task EnsureOwnerAsync(string email, CancellationToken ct)
    {
        if (await FindAsync(AppUser.OwnerId, ct) is { } owner) owner.BecomeOwnerAs(email);
        else db.Users.Add(AppUser.CreateOwner(email, Now));
        await db.SaveChangesAsync(ct);
        await people.ProvideStarterDataAsync(AppUser.OwnerId, ct);
    }

    private DateTime Now => time.GetLocalNow().DateTime;

    private Task<AppUser?> FindAsync(Guid id, CancellationToken ct) => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    // Empty removes the key. It's stored encrypted and never sent back.
    private string? Protect(string? key) => string.IsNullOrWhiteSpace(key) ? null : secrets.Protect(key);

    private static Person ToPerson(AppUser u) =>
        new(u.Id, u.Email, u.Name, u.IsOwner, u.Enabled, u.GeminiKeySetBy, u.AddedAt, u.LastSignInAt);
}
