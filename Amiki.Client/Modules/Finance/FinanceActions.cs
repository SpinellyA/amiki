using MudBlazor;

namespace Amiki.Modules.Finance;

public sealed class FinanceActions(FinanceStore store, IDialogService dialogs, ISnackbar snackbar)
{
    /// <summary>Opens the transaction dialog on a copy; returns whether it was saved.</summary>
    public async Task<bool> EditAsync(Transaction? tx = null)
    {
        var isNew = tx is null || !store.All.Contains(tx);
        var draft = tx?.Clone() ?? new Transaction { Category = Categories.DefaultFor(TxKind.Expense) };
        var parameters = new DialogParameters<TransactionDialog> { { d => d.Item, draft } };
        var dialog = await dialogs.ShowAsync<TransactionDialog>(isNew ? "New transaction" : "Edit transaction", parameters,
            new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true });
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
