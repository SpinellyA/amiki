namespace Amiki.Contracts.Assistant;

/// <param name="Now">Your local time, so "today" and "this week" mean yours, not the server's.</param>
/// <param name="History">The conversation so far (only the most recent messages are used).</param>
public sealed record ChatRequest(DateTime Now, IReadOnlyList<ChatLine> History, string Message)
{
    public const int MaxMessage = 4000;
    public const int MaxHistory = 16;

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Message) ? "Say something first."
        : Message.Length > MaxMessage ? $"That message is too long ({MaxMessage} characters max)."
        : null;
}
