namespace Amiki.Domain.Wishlist;

/// <summary>What buying something for <see cref="Price"/> would do to a <see cref="Balance"/>.</summary>
public readonly record struct Affordability(decimal Price, decimal Balance)
{
    public bool CanAfford => Balance >= Price;

    /// <summary>What's left after buying it (negative when you can't).</summary>
    public decimal LeftAfter => Balance - Price;

    /// <summary>How much more you'd need; zero when you can afford it.</summary>
    public decimal Short => CanAfford ? 0 : Price - Balance;

    /// <summary>How much of the price your balance covers, 0 to 1.</summary>
    public double Covered => Price <= 0 ? 1 : (double)Math.Clamp(Balance / Price, 0, 1);
}
