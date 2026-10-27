using Amiki.Domain.Diary;
using Amiki.Domain.Review;

namespace Amiki.Api.Endpoints;

/// <summary>Reflect: /api/weekly-notes and /api/diary (the diary is never shown to the owner).</summary>
public static class ReflectionEndpoints
{
    public static void MapReflection(this RouteGroupBuilder api)
    {
        api.MapEntity<WeeklyNote>("weekly-notes");
        api.MapEntity<DiaryEntry>("diary");
    }
}
