using System.Globalization;
using System.Text;
using Amiki.Modules.Assistant;

namespace Amiki.Api.Assistant;

/// <summary>
/// Amiki's standing instructions, built from your settings. The settings page shows exactly this
/// text ("What she sees"), so nothing is hidden from you.
/// </summary>
public static class AssistantPrompt
{
    public const string DefaultPersonality = "Warm, upbeat and to the point, like a capable friend. Light humour is fine. Short answers unless asked for detail.";

    public static string Build(AssistantSettings settings, DateTime now, IReadOnlyList<ToolDefinition> tools)
    {
        var s = new StringBuilder();
        s.AppendLine("You are Amiki, the personal assistant inside the user's own app (also called Amiki), which tracks their tasks, ideas, money, inbox and weekly reflections.");
        s.AppendLine($"Personality: {(string.IsNullOrWhiteSpace(settings.Personality) ? DefaultPersonality : settings.Personality.Trim())}");
        s.AppendLine();
        s.AppendLine($"Now: {now.ToString("dddd, MMMM d, yyyy, h:mm tt", CultureInfo.InvariantCulture)} (the user's local time). Money is in Philippine pesos (₱).");

        var areas = tools.Select(t => t.Area).Distinct().ToList();
        s.AppendLine(areas.Count == 0
            ? "You can't look at any of the user's data right now (it's switched off in your settings); say so if they ask about it."
            : $"You can look up: {string.Join(", ", areas)}. Use the tools to check instead of guessing, and only call the ones you need.");
        s.AppendLine("You can't see the user's diary at all; it's private. If asked about it, say so kindly.");
        s.AppendLine("You can't change anything yet (no adding, editing or deleting): if asked, say how they can do it in the app.");
        s.AppendLine("Reply in plain text (no markdown symbols like ** or #). Use short lines or '•' bullets for lists. Match the user's language (English, Filipino or Taglish).");

        if (!string.IsNullOrWhiteSpace(settings.AboutMe))
        {
            s.AppendLine();
            s.AppendLine("About the user, in their own words:");
            s.AppendLine(settings.AboutMe.Trim());
        }
        return s.ToString();
    }
}
