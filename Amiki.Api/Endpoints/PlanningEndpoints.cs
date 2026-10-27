using Amiki.Domain.Ideas;
using Amiki.Domain.Inbox;
using Amiki.Domain.Tasks;

namespace Amiki.Api.Endpoints;

/// <summary>Plan: /api/inbox, /api/tasks, /api/ideas (list / save / delete each).</summary>
public static class PlanningEndpoints
{
    public static void MapPlanning(this RouteGroupBuilder api)
    {
        api.MapEntity<InboxItem>("inbox");
        api.MapEntity<TaskItem>("tasks");
        api.MapEntity<Idea>("ideas");
    }
}
