using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amiki.Tests.Integration;

/// <summary>The money rules hold at the API, whatever an app sends.</summary>
[Collection(AmikiAppCollection.Name)]
public class MoneyRulesTests(AmikiApp app)
{
    private static readonly JsonSerializerOptions Json = AmikiApp.Json;

    private static Transaction Lunch(string account = "Cash", string category = "Food") =>
        new() { Amount = 85, Kind = TxKind.Expense, Category = category, Account = account, Note = "Lunch" };

    [Fact]
    public async Task Money_must_use_a_real_account_and_category()
    {
        var (_, friend) = await app.AddPersonAsync();
        var client = app.As(friend);

        var noAccount = Lunch(account: "Swiss bank");
        var noCategory = Lunch(category: "Yachts");

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"api/transactions/{noAccount.Id}", noAccount, Json)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"api/transactions/{noCategory.Id}", noCategory, Json)).StatusCode);
    }

    [Fact]
    public async Task Every_money_change_lands_in_the_history_once()
    {
        var (_, friend) = await app.AddPersonAsync();
        var client = app.As(friend);
        var lunch = Lunch();

        (await client.PutAsJsonAsync($"api/transactions/{lunch.Id}", lunch, Json)).EnsureSuccessStatusCode();
        (await client.PutAsJsonAsync($"api/transactions/{lunch.Id}", lunch, Json)).EnsureSuccessStatusCode(); // a retry: not an edit
        lunch.Amount = 95;
        (await client.PutAsJsonAsync($"api/transactions/{lunch.Id}", lunch, Json)).EnsureSuccessStatusCode();

        var history = (await client.GetFromJsonAsync<List<AuditEntry>>("api/audit", Json))!.Where(e => e.EntityId == lunch.Id).ToList();
        Assert.Equal([AuditEntry.Edited, AuditEntry.Created], history.Select(e => e.Action));
        Assert.Contains("amount 85 → 95", history[0].Summary);
    }

    [Fact]
    public async Task A_category_in_use_cant_be_deleted()
    {
        var (_, friend) = await app.AddPersonAsync();
        var client = app.As(friend);
        var snacks = new Category { Name = "Snacks", Kind = TxKind.Expense };
        (await client.PutAsJsonAsync($"api/categories/{snacks.Id}", snacks, Json)).EnsureSuccessStatusCode();
        var chips = Lunch(category: "Snacks");
        (await client.PutAsJsonAsync($"api/transactions/{chips.Id}", chips, Json)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Conflict, (await client.DeleteAsync($"api/categories/{snacks.Id}")).StatusCode);
    }

    [Fact]
    public async Task A_recorded_balance_check_cant_be_changed()
    {
        var (_, friend) = await app.AddPersonAsync();
        var client = app.As(friend);
        var check = new BalanceCheck { Account = "Cash", Expected = 0, Actual = 0 };
        (await client.PutAsJsonAsync($"api/balance-checks/{check.Id}", check, Json)).EnsureSuccessStatusCode();

        check.Actual = 500;
        check.Expected = 500;
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"api/balance-checks/{check.Id}", check, Json)).StatusCode);
    }

    [Fact]
    public async Task The_health_check_answers()
    {
        var response = await app.As().GetAsync("health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
