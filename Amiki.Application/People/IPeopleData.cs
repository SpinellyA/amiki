namespace Amiki.Application.People;

/// <summary>
/// Work on a whole person's data, across the usual "only this request's person" boundary. Only
/// the owner's People features use it.
/// </summary>
public interface IPeopleData
{
    /// <summary>Gives someone the starter accounts and categories they don't have yet.</summary>
    Task ProvideStarterDataAsync(Guid userId, CancellationToken ct);

    /// <summary>Removes the person and every row that belongs to them, in one transaction.</summary>
    Task DeleteEverythingAsync(Guid userId, CancellationToken ct);

    /// <summary>That person's assistant settings, created with defaults if they have none.</summary>
    Task<Domain.Assistant.AssistantSettings> AssistantSettingsForAsync(Guid userId, CancellationToken ct);
}
