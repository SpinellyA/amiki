namespace Amiki.Domain.Assistant;

/// <summary>One message in a chat with Amiki.</summary>
/// <param name="FromAmiki">True for her replies, false for yours.</param>
/// <param name="Looked">What she looked at to answer (e.g. "tasks", "money"); empty for your messages.</param>
/// <param name="Proposals">Changes she suggested in this reply, with whether you did or skipped them.</param>
/// <param name="Seconds">How long her reply took, measured by the app (shown under it).</param>
/// <param name="Note">Anything unusual about how it was answered, e.g. a backup model stepped in.</param>
public sealed record ChatLine(bool FromAmiki, string Text, IReadOnlyList<string>? Looked = null, IReadOnlyList<AssistantProposal>? Proposals = null, double? Seconds = null, string? Note = null)
{
    /// <summary>
    /// The message as the model should remember it, including what became of her suggestions,
    /// so she doesn't think something happened when you skipped it (or the other way round).
    /// </summary>
    public string ForModel()
    {
        if (Proposals is not { Count: > 0 }) return Text;
        var outcomes = Proposals.Select(p => $"- {p.Describe()}: " + p.Status switch
        {
            ProposalStatus.Done => "the user confirmed it, so it's done.",
            ProposalStatus.Skipped => "the user skipped it, so it did NOT happen.",
            _ => "not confirmed yet, so it has NOT happened.",
        });
        return $"{Text}\n\n[Suggested changes]\n{string.Join("\n", outcomes)}";
    }
}
