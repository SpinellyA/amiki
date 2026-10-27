using Amiki.Domain;

namespace Amiki.Application.Entities;

/// <summary>
/// The rules for saving and deleting one kind of item through <see cref="EntityService{T}"/>.
/// By default there are none beyond the item's own <see cref="IEntity.Validate"/>. A kind with
/// more (money, the diary...) derives from this and overrides only what it needs; register it
/// in <see cref="DependencyInjection"/>.
/// </summary>
public class EntityRules<T> where T : class, IEntity
{
    /// <summary>Checks that need the database, e.g. "that account exists". Broken: 400.</summary>
    public virtual Task<string?> CheckAsync(T incoming, CancellationToken ct) => Task.FromResult<string?>(null);

    /// <summary>Rules comparing the stored item with the incoming one, e.g. "archived plans are read-only". Broken: 409.</summary>
    public virtual string? CheckReplace(T stored, T incoming) => null;

    /// <summary>Rules for removing a stored item, e.g. "nothing still uses it". Broken: 409.</summary>
    public virtual Task<string?> CheckDeleteAsync(T stored, CancellationToken ct) => Task.FromResult<string?>(null);

    /// <summary>Adjusts a save from someone other than the owner (the stored item, the incoming one).</summary>
    public virtual void FromMember(T? stored, T incoming) { }

    /// <summary>Never shown when the owner is viewing someone's Amiki (the diary).</summary>
    public virtual bool PrivateFromOwner => false;

    /// <summary>Set to log every real change in the audit log under this name, e.g. "transactions".</summary>
    public virtual string? AuditAs => null;

    /// <summary>The readable line for the audit log, e.g. "Spent ₱85 · Food · Cash".</summary>
    public virtual string Describe(T item) => item.Id.ToString();
}
