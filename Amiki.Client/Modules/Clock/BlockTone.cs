namespace Amiki.Modules.Clock;

/// <summary>
/// The colour a block is drawn in: the same for every block of one task (or one label), so a
/// task's time reads as one colour around the dial. The colours themselves are in app.css.
/// </summary>
public static class BlockTone
{
    private const int Tones = 8;

    public static string For(TimeBlock block) => For(block.TaskId?.ToString() ?? block.Label.Trim().ToLowerInvariant());

    public static string For(Guid? taskId, string label) => For(taskId?.ToString() ?? label.Trim().ToLowerInvariant());

    // A fixed sum, not string.GetHashCode (which changes every time the app loads).
    private static string For(string key) => $"tone-{(key.Aggregate(7, (sum, c) => unchecked(sum * 31 + c)) & int.MaxValue) % Tones}";
}
