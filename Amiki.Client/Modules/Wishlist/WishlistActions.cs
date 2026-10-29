using Amiki.Modules.Finance;
using MudBlazor;

namespace Amiki.Modules.Wishlist;

/// <summary>
/// What you can do with wishlist items, with the dialogs and "Undo" that go with it. Every move of
/// wishlist money goes through <see cref="WishlistFund"/>, so the pool and the items always add up.
/// </summary>
public sealed class WishlistActions(WishlistStore store, FinanceStore finance, FinanceActions money, CategoryStore categories,
    IDialogService dialogs, ISnackbar snackbar)
{
    private static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    /// <summary>The category a purchase is logged under by default, if you have it.</summary>
    private const string ShoppingCategory = "Shopping";

    /// <summary>The wishlist against your money right now.</summary>
    public WishlistSummary Summary => new(store.All, store.Fund.Amount, finance.TotalBalance);

    /// <summary>
    /// Opens the item dialog on a copy: a new item when <paramref name="item"/> is null or not on
    /// the list yet (e.g. filled in by Amiki). A changed price goes into the item's price history;
    /// if it dropped below what's put toward it, the extra goes back to wishlist money.
    /// Returns whether it was saved.
    /// </summary>
    public async Task<bool> EditAsync(WishItem? item = null)
    {
        var isNew = item is null || store.Get(item.Id) is null;
        var draft = item?.Clone() ?? new WishItem();
        if (isNew) draft.Rank = NextRank();
        var parameters = new DialogParameters<WishItemDialog> { { d => d.Item, draft }, { d => d.IsNew, isNew } };
        var dialog = await dialogs.ShowAsync<WishItemDialog>(isNew ? "Add to wishlist" : "Edit item", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: WishItem edited }) return false;

        if (isNew)
        {
            store.Upsert(edited);
            snackbar.Add($"Added “{edited.Name}”", Severity.Normal);
            return true;
        }

        var current = store.Get(edited.Id) ?? item!;
        var saved = edited.Clone();
        saved.Price = current.Price;
        saved.PriceHistory = current.PriceHistory;
        saved = saved.Repriced(edited.Price, DateTime.Now);
        var over = saved.Saved - saved.Price;
        store.Apply(over > 0 ? store.Fund.PutToward(saved, -over, DateTime.Now) : new(store.Fund, saved));
        return true;
    }

    /// <summary>Opens "Put money toward it" (or take it back).</summary>
    public async Task PutTowardAsync(WishItem item)
    {
        if (store.Get(item.Id) is not { IsOpen: true } current) return;
        var parameters = new DialogParameters<PutTowardDialog> { { d => d.Item, current }, { d => d.Pool, Summary.Pool } };
        var dialog = await dialogs.ShowAsync<PutTowardDialog>(current.Name, parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: decimal amount } || amount == 0) return;

        var before = (store.Fund, current);
        var change = store.Fund.PutToward(current, amount, DateTime.Now);
        store.Apply(change);
        var moved = change.Item.Saved - current.Saved;
        WithUndo(moved >= 0
                ? $"Put {Money.Format(moved)} toward “{current.Name}” · {Money.Format(change.Item.Saved)} of {Money.Format(current.Price)}"
                : $"Took {Money.Format(-moved)} back from “{current.Name}”",
            () => store.Apply(new(before.Fund, before.current)));
    }

    /// <summary>Moves an item to another status. Skipping it gives its money back to wishlist money.</summary>
    public void SetStatus(WishItem item, WishStatus status)
    {
        if (store.Get(item.Id) is not { } current || current.Status == status) return;
        var beforeFund = store.Fund;
        var change = status == WishStatus.Skipped
            ? store.Fund.Release(current, DateTime.Now)
            : new WishlistChange(store.Fund, current);
        var moved = change.Item.WithStatus(status, DateTime.Now);
        if (status == WishStatus.Saving) moved.Rank = NextRank();
        store.Apply(change with { Item = moved });
        WithUndo($"“{current.Name}” moved to {WishStatusLook.Name(status)}"
                 + (current.Saved > 0 && status == WishStatus.Skipped ? $" · {Money.Format(current.Saved)} back in wishlist money" : ""),
            () => store.Apply(new(beforeFund, current)));
    }

    /// <summary>Moves a saving-for item up or down your order.</summary>
    public void Move(WishItem item, int by)
    {
        var order = Summary.SavingInOrder.ToList();
        var from = order.FindIndex(i => i.Id == item.Id);
        var to = Math.Clamp(from + by, 0, order.Count - 1);
        if (from < 0 || from == to) return;
        order.RemoveAt(from);
        order.Insert(to, item);
        store.Reorder(order);
    }

    /// <summary>
    /// Logs the purchase in Money (prefilled with the price, your usual account, and Shopping if you
    /// have it), then marks it bought: paid from what was put toward it first, then wishlist money.
    /// Something added to Want moments ago gets a gentle "sure?" first. Cancel leaves it as it was.
    /// </summary>
    public async Task BuyAsync(WishItem item)
    {
        if (store.Get(item.Id) is not { IsOpen: true } current) return;
        if (current.IsFreshWant(DateTime.Now) && await dialogs.ShowMessageBoxAsync("Buy it already?",
                $"You added “{current.Name}” to Want {Amiki.Components.When.Short(current.CreatedAt).ToLowerInvariant()}. Waiting a few days is a good way to be sure you still want it.",
                yesText: "Yes, I bought it", cancelText: "Not yet") != true)
            return;

        var purchase = new Transaction
        {
            Amount = current.Price,
            Kind = TxKind.Expense,
            Category = categories.Pickable(TxKind.Expense).Any(c => c.Name == ShoppingCategory) ? ShoppingCategory : money.DefaultCategory(TxKind.Expense),
            Note = current.Name.Length > Transaction.MaxNote ? current.Name[..Transaction.MaxNote] : current.Name,
        };
        if (!await money.EditAsync(purchase)) return;

        var pool = store.Fund.Amount;
        var change = store.Fund.Buy(current, purchase.Amount, purchase.Id, DateTime.Now);
        store.Apply(change);
        var fromWishlist = current.Saved + pool - change.Fund.Amount;
        snackbar.Add($"Bought “{current.Name}” · logged {Money.Format(purchase.Amount)} in Money"
            + (fromWishlist > 0 ? $" · {Money.Format(Math.Min(fromWishlist, purchase.Amount))} from wishlist money" : ""), Severity.Success);
    }

    /// <summary>Sets how much wishlist money isn't put toward anything yet, and the income rule.</summary>
    public async Task SetFundAsync()
    {
        var parameters = new DialogParameters<WishlistFundDialog>
        {
            { d => d.Amount, store.Fund.Amount },
            { d => d.Balance, finance.TotalBalance },
            { d => d.SavedInItems, Summary.SavedInItems },
            { d => d.IncomeShare, store.Fund.IncomeShare },
        };
        var dialog = await dialogs.ShowAsync<WishlistFundDialog>("Wishlist money", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: ValueTuple<decimal, int?> chosen }) return;
        var (amount, share) = chosen;

        var before = store.Fund;
        store.SaveFund(store.Fund.SetTo(amount, DateTime.Now).WithIncomeShare(share, DateTime.Now));
        WithUndo($"Wishlist money set to {Money.Format(store.Fund.Amount)}", () => store.SaveFund(before));
    }

    /// <summary>Adds to wishlist money (e.g. a share of income just logged).</summary>
    public void AddToFund(decimal amount, string why)
    {
        if (amount <= 0) return;
        var before = store.Fund;
        store.SaveFund(store.Fund.Add(amount, DateTime.Now));
        WithUndo($"{Money.Format(amount)} {why} added to wishlist money", () => store.SaveFund(before));
    }

    /// <summary>Removes an item; anything put toward it goes back to wishlist money.</summary>
    public void DeleteWithUndo(WishItem item)
    {
        if (store.Get(item.Id) is not { } current) return;
        var beforeFund = store.Fund;
        var change = store.Fund.Release(current, DateTime.Now);
        if (change.Fund.Amount != beforeFund.Amount) store.SaveFund(change.Fund);
        store.Remove(current);
        WithUndo($"Removed “{current.Name}”", () => store.Apply(new(beforeFund, current)));
    }

    private int NextRank() => store.All.Where(i => i.Status == WishStatus.Saving).Select(i => i.Rank + 1).DefaultIfEmpty(0).Max();

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
