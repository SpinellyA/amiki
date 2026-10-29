using Amiki.Modules.Finance;
using MudBlazor;

namespace Amiki.Modules.Wishlist;

/// <summary>What you can do with wishlist items, with the dialogs and "Undo" that go with it.</summary>
public sealed class WishlistActions(WishlistStore store, FinanceActions money, CategoryStore categories, IDialogService dialogs, ISnackbar snackbar)
{
    private static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    /// <summary>The category a purchase is logged under by default, if you have it.</summary>
    private const string ShoppingCategory = "Shopping";

    /// <summary>Opens the item dialog on a copy (a new item when <paramref name="item"/> is null).</summary>
    public async Task EditAsync(WishItem? item = null)
    {
        var isNew = item is null;
        var draft = item?.Clone() ?? new WishItem();
        var parameters = new DialogParameters<WishItemDialog> { { d => d.Item, draft } };
        var dialog = await dialogs.ShowAsync<WishItemDialog>(isNew ? "Add to wishlist" : "Edit item", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: WishItem saved }) return;

        store.Upsert(saved);
        if (isNew) snackbar.Add($"Added “{saved.Name}”", Severity.Normal);
    }

    public void SetStatus(WishItem item, WishStatus status)
    {
        if (store.Get(item.Id) is not { } current || current.Status == status) return;
        store.Upsert(current.WithStatus(status, DateTime.Now));
        WithUndo($"“{current.Name}” moved to {WishStatusLook.Name(status)}", () => store.Upsert(current));
    }

    /// <summary>
    /// Logs the purchase in Money (prefilled with the price, your usual account, and Shopping if you
    /// have it), then marks the item bought and linked to that expense. Cancel leaves it as it was.
    /// </summary>
    public async Task BuyAsync(WishItem item)
    {
        var purchase = new Transaction
        {
            Amount = item.Price,
            Kind = TxKind.Expense,
            Category = categories.Pickable(TxKind.Expense).Any(c => c.Name == ShoppingCategory) ? ShoppingCategory : money.DefaultCategory(TxKind.Expense),
            Note = item.Name.Length > Transaction.MaxNote ? item.Name[..Transaction.MaxNote] : item.Name,
        };
        if (!await money.EditAsync(purchase)) return;

        store.Upsert(item.BoughtWith(purchase.Id, DateTime.Now));
        snackbar.Add($"Bought “{item.Name}” · logged {Money.Format(purchase.Amount)} in Money", Severity.Success);
    }

    public void DeleteWithUndo(WishItem item)
    {
        store.Remove(item);
        WithUndo($"Removed “{item.Name}”", () => store.Upsert(item));
    }

    private void WithUndo(string message, Action undo) =>
        snackbar.Add(message, Severity.Normal, c =>
        {
            c.Action = "Undo";
            c.ActionColor = Color.Primary;
            c.OnClick = _ =>
            {
                undo();
                return Task.CompletedTask;
            };
        });
}
