using System.Text.Json.Nodes;
using Amiki.Application.Data;
using Amiki.Application.Finance;
using Amiki.Domain.Assistant;
using Amiki.Domain.Wishlist;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant.Tools.Lookups;

public sealed class ListWishlistTool(IAmikiDb db, MoneyBook money) : ILookupTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("list_wishlist",
        "The user's wishlist: things they want to buy, with price, status (want, saving, bought, skipped) and whether their wishlist money covers each. Wishlist money is the part of their balance they set aside for these; judge affordability against it, not the whole balance. Use for 'can I afford…', 'what am I saving for', or advice on what to buy first.",
        AssistantArea.Money, """
        {"type":"object","properties":{
          "include_done":{"type":"boolean","description":"Also list bought and skipped items"}}}
        """);

    public async Task<JsonNode> RunAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var balance = (await money.AllAsync(ct)).TotalBalance;
        var fund = (await db.WishlistFunds.AsNoTracking().FirstOrDefaultAsync(ct))?.Amount ?? 0;
        var items = await db.Wishlist.AsNoTracking().OrderBy(w => w.Status).ThenBy(w => w.Price).ToListAsync(ct);
        var shown = args.Flag("include_done") ? items : items.Where(w => w.IsOpen).ToList();
        var summary = new WishlistSummary(items, fund, balance);

        var result = ToolResult.Rows(shown.Select(w =>
        {
            var afford = w.Against(fund);
            return new JsonObject
            {
                ["name"] = w.Name,
                ["price"] = w.Price,
                ["status"] = w.Status.ToString().ToLowerInvariant(),
                ["wishlist_money_covers_it"] = afford.CanAfford,
                ["short_by"] = afford.CanAfford ? null : afford.Short,
                ["note"] = w.Note.Length > 0 ? w.Note : null,
            };
        }));
        result["wishlist_money"] = fund;
        result["total_balance"] = balance;
        result["saving_for_total"] = summary.SavingFor;
        result["wishlist_money_left_after_buying_everything_saving_for"] = summary.AfterSaving.LeftAfter;
        return result;
    }
}
