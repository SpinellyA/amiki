using Amiki.Application.Entities;
using Amiki.Domain.Finance;

namespace Amiki.Application.Finance;

/// <summary>Archived plans are read-only until restored.</summary>
public sealed class PlanRules : EntityRules<Plan>
{
    public override string? CheckReplace(Plan stored, Plan incoming) => Plan.CheckReplace(stored, incoming);
}
