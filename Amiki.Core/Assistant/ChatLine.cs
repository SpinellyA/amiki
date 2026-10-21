namespace Amiki.Modules.Assistant;

/// <summary>One message in a chat with Amiki.</summary>
/// <param name="FromAmiki">True for her replies, false for yours.</param>
/// <param name="Looked">What she looked at to answer (e.g. "tasks", "money"); empty for your messages.</param>
public sealed record ChatLine(bool FromAmiki, string Text, IReadOnlyList<string>? Looked = null);
