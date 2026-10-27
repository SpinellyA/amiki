namespace Amiki.Application.Data;

/// <summary>Whether the database answers, for uptime monitors.</summary>
public interface IDatabaseHealth
{
    /// <summary>True when a trivial query comes back in time. Never throws.</summary>
    Task<bool> AnswersAsync(CancellationToken ct);
}
