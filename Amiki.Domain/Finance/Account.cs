namespace Amiki.Domain.Finance;

/// <summary>A place money lives (Cash, GCash...). Its balance is worked out by the <see cref="Ledger"/>, never stored.</summary>
public sealed record Account(string Name, decimal OpeningBalance);
