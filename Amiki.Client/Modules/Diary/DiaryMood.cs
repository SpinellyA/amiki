using MudBlazor;

namespace Amiki.Modules.Diary;

/// <summary>How each mood (1–5) is shown.</summary>
public static class DiaryMood
{
    public static IEnumerable<int> All => Enumerable.Range(DiaryEntry.LowestMood, DiaryEntry.HighestMood - DiaryEntry.LowestMood + 1);

    public static string Name(int mood) => mood switch
    {
        1 => "Rough",
        2 => "Meh",
        3 => "Okay",
        4 => "Good",
        _ => "Great",
    };

    public static string Icon(int mood) => mood switch
    {
        1 => Icons.Material.Outlined.SentimentVeryDissatisfied,
        2 => Icons.Material.Outlined.SentimentDissatisfied,
        3 => Icons.Material.Outlined.SentimentNeutral,
        4 => Icons.Material.Outlined.SentimentSatisfied,
        _ => Icons.Material.Outlined.SentimentVerySatisfied,
    };

    /// <summary>CSS class for the mood's colour, from rough (warm) to great (green).</summary>
    public static string CssClass(int? mood) => mood is { } m ? $"mood-{m}" : "mood-none";
}
