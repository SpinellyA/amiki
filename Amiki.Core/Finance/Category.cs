using Amiki.Core;

namespace Amiki.Modules.Finance;

/// <summary>
/// A label for money in or out, e.g. "Food" for spending or "Allowance" for income. Yours to add,
/// rename and delete. Transactions refer to a category by name, so renaming or deleting one also
/// re-saves its transactions (the app does that, as normal audited edits).
/// </summary>
public sealed class Category : IEntity
{
    // Built in: the app itself files money under these, so they can't be renamed or deleted.
    /// <summary>Where spending goes when its category is deleted.</summary>
    public const string OtherExpense = "Other";
    /// <summary>Where income goes when its category is deleted.</summary>
    public const string OtherIncome = "Other income";
    /// <summary>Transfer fees.</summary>
    public const string Fees = "Fees";
    /// <summary>Corrections from balance checks: bring Amiki in line with reality, not real spending or income.</summary>
    public const string BalanceFix = "Balance fix";

    public const int MaxNameLength = 40;

    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    /// <summary>Spending or income. Fixed once created, since its transactions share it.</summary>
    public TxKind Kind { get; set; }
    /// <summary>A key from the app's icon list (not the icon itself), e.g. "Restaurant".</summary>
    public string Icon { get; set; } = "Label";
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public bool IsBuiltIn => IsBuiltInName(Name);

    public static bool IsBuiltInName(string name) => name is OtherExpense or OtherIncome or Fees or BalanceFix;

    /// <summary>Corrections are left out of spending/income totals and charts (they're still in the list and history).</summary>
    public static bool IsCorrection(string name) => name == BalanceFix;

    public static string FallbackFor(TxKind kind) => kind == TxKind.Income ? OtherIncome : OtherExpense;

    public Category Clone() => (Category)MemberwiseClone();

    public string? Validate() =>
        string.IsNullOrWhiteSpace(Name) ? "Give the category a name."
        : Name != Name.Trim() ? "The name can't start or end with a space."
        : Name.Length > MaxNameLength ? $"Name is too long ({MaxNameLength} characters max)."
        : string.IsNullOrWhiteSpace(Icon) || Icon.Length > 40 ? "Pick an icon."
        : null;

    /// <summary>Rules comparing the saved category with an edit of it.</summary>
    public static string? CheckReplace(Category stored, Category incoming) =>
        incoming.Kind != stored.Kind ? "A category can't switch between spending and income."
        : stored.IsBuiltIn && incoming.Name != stored.Name ? $"“{stored.Name}” is built in, so it can't be renamed."
        : null;

    public string Describe() => $"{(Kind == TxKind.Income ? "Income" : "Spending")} category “{Name}”";
}
