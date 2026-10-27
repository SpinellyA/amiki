namespace Amiki.Domain.Assistant;

/// <summary>
/// How hard Amiki thinks before answering in chat. More thinking can help with tricky questions
/// but makes every reply slower (and uses more of the free tier). Measured on 3.8 Flash for a
/// two-line answer: the model's default spent ~430 thinking tokens and took ~9 s; "low", ~5 s.
/// </summary>
public static class ThinkingLevels
{
    /// <summary>Let the model decide (it tends to think a lot).</summary>
    public const string ModelDefault = "";
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";

    public static readonly IReadOnlyList<string> All = [Low, Medium, High, ModelDefault];

    public static bool IsKnown(string level) => All.Contains(level);

    public static string Name(string level) => level switch
    {
        Low => "Quick",
        Medium => "Balanced",
        High => "Careful",
        _ => "Model's default",
    };

    public static string Hint(string level) => level switch
    {
        Low => "Fastest replies. Plenty for lookups and logging things.",
        Medium => "A bit slower; better at comparisons and planning.",
        High => "Slowest; for tricky questions.",
        _ => "Whatever the model does by itself (on 3.8 Flash, usually lots of thinking).",
    };
}
