using MudBlazor;

namespace Amiki.Modules.Finance;

/// <summary>Setting, changing and turning off budgets, with the dialog and "Undo".</summary>
public sealed class BudgetActions(BudgetStore budgets, FinanceStore finance, IDialogService dialogs, ISnackbar snackbar)
{
    private static readonly DialogOptions Options = new() { MaxWidth = MaxWidth.Small, FullWidth = true, CloseOnEscapeKey = true };

    public async Task SetAsync(MoneyPeriod period)
    {
        var current = budgets.Book.For(period);
        var parameters = new DialogParameters<BudgetDialog>
        {
            { d => d.Period, period },
            { d => d.Amount, current?.Amount ?? 0 },
            { d => d.Repeats, current?.Repeats ?? true },
            { d => d.Balance, finance.TotalBalance },
            { d => d.HasBudget, current is not null },
        };
        var dialog = await dialogs.ShowAsync<BudgetDialog>($"Budget for {period.Label}", parameters, Options);
        if (await dialog.Result is not { Canceled: false, Data: BudgetDialog.Choice choice }) return;

        var before = budgets.Own(period);
        budgets.Save(Budget.For(period, choice.Amount, choice.Repeats));
        snackbar.Add(choice.Amount > 0 ? $"Budget for {period.Name}: {Money.Format(choice.Amount)}" : $"No budget from {period.Name} on",
            Severity.Normal, c =>
            {
                c.Action = "Undo";
                c.ActionColor = Color.Primary;
                c.OnClick = _ =>
                {
                    if (before is null) budgets.Remove(Budget.For(period, 0, true));
                    else budgets.Save(before);
                    return Task.CompletedTask;
                };
            });
    }
}
