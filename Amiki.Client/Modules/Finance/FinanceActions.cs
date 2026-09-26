using MudBlazor;

namespace Amiki.Modules.Finance;

public sealed class FinanceActions(FinanceStore store, IDialogService dialogs, ISnackbar snackbar)
{
    private static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    /// <summary>Opens the transaction dialog on a copy; returns whether it was saved.</summary>
    public async Task<bool> EditAsync(Transaction? tx = null)
    {
        var isNew = tx is null || store.All.All(t => t.Id != tx.Id);
        var draft = tx?.Clone() ?? new Transaction { Category = Categories.DefaultFor(TxKind.Expense) };
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
