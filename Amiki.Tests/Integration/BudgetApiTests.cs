using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amiki.Tests.Integration;

[Collection(AmikiAppCollection.Name)]
public class BudgetApiTests(AmikiApp app)
{
    private static readonly JsonSerializerOptions Json = AmikiApp.Json;
    private static readonly MoneyPeriod November = new(PeriodLength.Month, new DateTime(2026, 11, 1));

    [Fact]
    public async Task A_budget_saves_and_comes_back_as_it_was()
    {
        var (_, friend) = await app.AddPersonAsync();
        var budget = Budget.For(November, 8000, repeats: true);

        (await app.As(friend).PutAsJsonAsync($"api/budgets/{budget.Id}", budget, Json)).EnsureSuccessStatusCode();

        var saved = Assert.Single((await app.As(friend).GetFromJsonAsync<List<Budget>>("api/budgets", Json))!);
        Assert.Equal((PeriodLength.Month, November.Start, 8000m, true), (saved.Length, saved.Start, saved.Amount, saved.Repeats));
        Assert.Empty((await app.As().GetFromJsonAsync<List<Budget>>("api/budgets", Json))!.Where(b => b.Id == budget.Id && b.Amount == 8000));
    }

    [Fact]
    public async Task A_budget_that_doesnt_start_its_period_is_refused()
    {
        var bad = new Budget { Id = Budget.IdFor(November), Length = PeriodLength.Month, Start = new DateTime(2026, 11, 5), Amount = 100 };

        var response = await app.As().PutAsJsonAsync($"api/budgets/{bad.Id}", bad, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
