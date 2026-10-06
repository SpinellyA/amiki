using MudBlazor;

namespace Amiki.Modules.Ideas;

/// <summary>How each idea status is shown: its name and icon.</summary>
public static class IdeaStatusLook
{
    public static string Name(IdeaStatus status) => status switch
    {
        IdeaStatus.InProgress => "In progress",
        IdeaStatus.Done => "Done",
        _ => "Parked",
    };

    public static string Icon(IdeaStatus status) => status switch
    {
        IdeaStatus.InProgress => Icons.Material.Outlined.Autorenew,
        IdeaStatus.Done => Icons.Material.Outlined.CheckCircle,
        _ => Icons.Material.Outlined.Inventory2,
    };
}
