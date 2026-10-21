namespace Amiki.Modules.Assistant;

/// <param name="Reply">Her answer.</param>
/// <param name="Looked">The areas she looked at to answer, e.g. "tasks", "money".</param>
public sealed record ChatReply(string Reply, IReadOnlyList<string> Looked);
