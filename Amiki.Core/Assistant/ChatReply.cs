namespace Amiki.Modules.Assistant;

/// <param name="Reply">Her answer.</param>
/// <param name="Looked">The areas she looked at to answer, e.g. "tasks", "money".</param>
/// <param name="Proposals">Changes she suggests, waiting for you to confirm.</param>
/// <param name="Note">Anything unusual about how it was answered (e.g. a backup model stepped in).</param>
public sealed record ChatReply(string Reply, IReadOnlyList<string> Looked, IReadOnlyList<AssistantProposal> Proposals, string? Note = null);
