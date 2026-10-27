using Amiki.Application.Data;
using Amiki.Application.Entities;
using Amiki.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Finance;

/// <summary>
/// Category names are unique per kind (ignoring case). Built-in ones stay; others go only once
/// their transactions have moved elsewhere. Every change is audited.
/// </summary>
public sealed class CategoryRules(IAmikiDb db) : EntityRules<Category>
{
    public override async Task<string?> CheckAsync(Category incoming, CancellationToken ct)
    {
        var name = incoming.Name.ToLower();
        return await db.Categories.AnyAsync(c => c.Id != incoming.Id && c.Kind == incoming.Kind && c.Name.ToLower() == name, ct)
            ? $"There's already a category called “{incoming.Name}”."
            : null;
    }

    public override string? CheckReplace(Category stored, Category incoming) => Category.CheckReplace(stored, incoming);

    public override async Task<string?> CheckDeleteAsync(Category stored, CancellationToken ct)
    {
        if (stored.IsBuiltIn) return $"“{stored.Name}” is built in, so it can't be deleted.";
        var used = await db.Transactions.CountAsync(t => t.Category == stored.Name && t.Kind == stored.Kind, ct);
        return used == 0 ? null : $"“{stored.Name}” still has {used} transaction{(used == 1 ? "" : "s")}. Move them to another category first.";
    }

    public override string AuditAs => "categories";

    public override string Describe(Category item) => item.Describe();
}
