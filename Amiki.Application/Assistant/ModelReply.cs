namespace Amiki.Application.Assistant;

/// <summary>A chat answer from the model.</summary>
/// <param name="Note">Anything worth telling the user about how it was answered, e.g. that a backup
/// model stepped in because the main one hit its daily limit. Null when nothing unusual happened.</param>
public sealed record ModelReply(string Text, string? Note = null);
