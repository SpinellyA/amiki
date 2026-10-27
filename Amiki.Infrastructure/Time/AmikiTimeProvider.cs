namespace Amiki.Infrastructure.Time;

/// <summary>
/// The clock in Amiki's time zone. The server runs in UTC, but "today", "this week" and every
/// timestamp in Amiki mean the people's local time (Philippine time by default).
/// Use <c>time.GetLocalNow().DateTime</c> wherever the client-side code would say DateTime.Now.
/// </summary>
public sealed class AmikiTimeProvider(TimeZoneInfo zone) : TimeProvider
{
    public const string DefaultZone = "Asia/Manila";

    public override TimeZoneInfo LocalTimeZone => zone;

    /// <summary>The named zone, or a fixed UTC+8 if this machine doesn't know it.</summary>
    public static AmikiTimeProvider For(string? zoneId)
    {
        try
        {
            return new AmikiTimeProvider(TimeZoneInfo.FindSystemTimeZoneById(zoneId ?? DefaultZone));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return new AmikiTimeProvider(TimeZoneInfo.CreateCustomTimeZone("Amiki+8", TimeSpan.FromHours(8), "UTC+8", "UTC+8"));
        }
    }
}
