namespace Amiki.Core;

/// <summary>
/// Anything the API stores. Ids are generated on the client, so a phone can create items
/// offline and sending the same save twice is harmless (PUT to the same id is an upsert).
/// </summary>
public interface IEntity
{
    Guid Id { get; }

    /// <summary>Null when valid; otherwise the reason, which the API returns as a 400.</summary>
    string? Validate();
}
