using MudBlazor;

namespace Amiki.Modules.Finance;

public sealed class FinanceActions(FinanceStore store, CategoryStore categories, IDialogService dialogs, ISnackbar snackbar)
{
    private static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    /// <summary>Opens the transaction dialog on a copy; returns whether it was saved.</summary>
    public async Task<bool> EditAsync(Transaction? tx = null)
    {
        var isNew = tx is null || store.All.All(t => t.Id != tx.Id);
        var draft = tx?.Clone() ?? new Transaction { Category = DefaultCategory(TxKind.Expense) };
        var parameters = new DialogParameters<TransactionDialog> { { d => d.Item, draft } };
        var dialog = await dialogs.ShowAsync<TransactionDialog>(isNew ? "New transaction" : "Edit transaction", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: Transaction saved }) return false;

        store.Upsert(saved);
        return true;
    }

    public void Log(Transaction tx)
    {
        store.Upsert(tx);
        WithUndo($"Logged {Money.Signed(tx.Signed)} · {tx.Category} · {tx.Account}", () => store.Remove(tx));
    }

    public void DeleteWithUndo(Transaction tx)
    {
        store.Remove(tx);
        WithUndo("Transaction deleted", () => store.Upsert(tx));
    }

    /// <summary>Opens "Move money", optionally editing an existing transfer or starting from an account.</summary>
    public async Task MoveAsync(Transfer? existing = null, string? from = null)
    {
        var isNew = existing is null || store.Transfers.All(t => t.Id != existing.Id);
        var draft = existing?.Clone() ?? new Transfer { From = from ?? "" };
        var parameters = new DialogParameters<MoveMoneyDialog> { { d => d.Item, draft } };
        var dialog = await dialogs.ShowAsync<MoveMoneyDialog>(isNew ? "Move money" : "Edit transfer", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: Transfer saved }) return;

        store.Move(saved);
        var message = $"Moved {Money.Format(saved.Amount)} from {saved.From} to {saved.To}" + (saved.Fee > 0 ? $" · fee {Money.Format(saved.Fee)}" : "");
        if (isNew) WithUndo(message, () => store.RemoveTransfer(saved));
        else snackbar.Add("Transfer updated", Severity.Normal);
    }

    public void DeleteTransferWithUndo(Transfer transfer)
    {
        store.RemoveTransfer(transfer);
        WithUndo("Transfer deleted", () => store.Move(transfer));
    }

    /// <summary>Opens "Check balances" (all accounts, or one) and reports what it found.</summary>
    public async Task CheckBalancesAsync(string? onlyAccount = null)
    {
        var parameters = new DialogParameters<BalanceCheckDialog> { { d => d.OnlyAccount, onlyAccount } };
        var dialog = await dialogs.ShowAsync<BalanceCheckDialog>(onlyAccount is null ? "Check balances" : $"Check {onlyAccount}", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: List<BalanceCheck> checks } || checks.Count == 0) return;

        var corrected = checks.Where(c => !c.Matched).ToList();
        snackbar.Add(corrected.Count == 0
                ? $"Checked {Plural(checks.Count, "account")}. Everything matched."
                : $"Checked {Plural(checks.Count, "account")}. Corrected {string.Join(", ", corrected.Select(c => $"{c.Account} by {Money.Signed(c.Difference)}"))}.",
            corrected.Count == 0 ? Severity.Success : Severity.Normal);
    }

    /// <summary>The category you've used most for this direction lately, so logging starts on your usual one.</summary>
    public string DefaultCategory(TxKind kind)
    {
        var pickable = categories.Pickable(kind).Select(c => c.Name).ToList();
        var since = DateTime.Today.AddDays(-60);
        return store.All.Where(t => t.Kind == kind && t.Date >= since && pickable.Contains(t.Category))
                   .GroupBy(t => t.Category).MaxBy(g => g.Count())?.Key
               ?? pickable.FirstOrDefault()
               ?? Category.FallbackFor(kind);
    }

    /// <summary>Returns why it can't be added, or null once it's added.</summary>
    public string? AddCategory(string name, TxKind kind, string icon)
    {
        var category = new Category { Name = name.Trim(), Kind = kind, Icon = icon };
        if ((category.Validate() ?? NameProblem(category)) is { } problem) return problem;
        categories.Save(category);
        snackbar.Add($"Added “{category.Name}”", Severity.Normal);
        return null;
    }

    /// <summary>Renames a category and moves its transactions along. Returns why it can't, or null once done.</summary>
    public string? RenameCategory(Category category, string newName)
    {
        var renamed = category.Clone();
        renamed.Name = newName.Trim();
        if (renamed.Name == category.Name) return null;
        if (category.IsBuiltIn) return $"“{category.Name}” is built in, so it can't be renamed.";
        if ((renamed.Validate() ?? NameProblem(renamed)) is { } problem) return problem;

        var moving = store.InCategory(category.Name, category.Kind).Select(t => t.Id).ToList();
        categories.Save(renamed); // the new name has to exist before transactions point at it
        store.Recategorize(moving, renamed.Name);
        snackbar.Add($"Renamed “{category.Name}” to “{renamed.Name}”" + (moving.Count > 0 ? $" · {Plural(moving.Count, "transaction")} updated" : ""), Severity.Normal);
        return null;
    }

    public void SetCategoryIcon(Category category, string icon)
    {
        var changed = category.Clone();
        changed.Icon = icon;
        categories.Save(changed);
    }

    /// <summary>Deletes a category. If transactions use it, first asks where they should move.</summary>
    public async Task DeleteCategoryAsync(Category category)
    {
        if (category.IsBuiltIn) return;
        var moving = store.InCategory(category.Name, category.Kind).Select(t => t.Id).ToList();
        var moveTo = Category.FallbackFor(category.Kind);
        if (moving.Count > 0)
        {
            var parameters = new DialogParameters<DeleteCategoryDialog> { { d => d.Item, category }, { d => d.Count, moving.Count } };
            var dialog = await dialogs.ShowAsync<DeleteCategoryDialog>($"Delete “{category.Name}”?", parameters, Options);
            if (await dialog.Result is not { Canceled: false, Data: string picked }) return;
            moveTo = picked;
        }
        store.Recategorize(moving, moveTo); // the server only deletes a category nothing uses
        categories.Remove(category);
        WithUndo($"Deleted “{category.Name}”" + (moving.Count > 0 ? $" · {Plural(moving.Count, "transaction")} moved to {moveTo}" : ""), () =>
        {
            categories.Save(category);
            store.Recategorize(moving, category.Name);
        });
    }

    private string? NameProblem(Category category) =>
        categories.NameTaken(category.Name, category.Kind, category.Id) ? $"There's already a category called “{category.Name}”." : null;

    private static string Plural(int n, string word) => $"{n} {word}{(n == 1 ? "" : "s")}";

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
