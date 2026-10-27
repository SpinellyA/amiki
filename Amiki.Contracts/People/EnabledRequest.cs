namespace Amiki.Contracts.People;

/// <summary>Turn someone's sign-in on or off (their data is kept either way).</summary>
public sealed record EnabledRequest(bool Enabled);
