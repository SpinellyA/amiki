using System.Globalization;

namespace Amiki.Modules.Clock;

/// <summary>
/// Where things go on the 24-hour dial: midnight at the top, noon at the bottom, clockwise.
/// Works in the dial's own SVG units (a <see cref="ViewBox"/>-wide square).
/// </summary>
public static class DialGeometry
{
    public const double ViewBox = 240;
    public const double Center = ViewBox / 2;
    public const double Ring = 92;

    public static (double X, double Y) Point(double minute, double radius)
    {
        var angle = minute / TimeBlock.MinutesInDay * 2 * Math.PI;
        return (Center + radius * Math.Sin(angle), Center - radius * Math.Cos(angle));
    }

    /// <summary>The SVG path of the ring's arc from one minute to another.</summary>
    public static string Arc(double fromMinute, double toMinute, double radius = Ring)
    {
        // A full circle can't be one arc; stop a hair short so a 24-hour block still draws.
        toMinute = Math.Min(toMinute, fromMinute + TimeBlock.MinutesInDay - 0.5);
        var (x1, y1) = Point(fromMinute, radius);
        var (x2, y2) = Point(toMinute, radius);
        var large = toMinute - fromMinute > TimeBlock.MinutesInDay / 2 ? 1 : 0;
        return $"M {N(x1)} {N(y1)} A {N(radius)} {N(radius)} 0 {large} 1 {N(x2)} {N(y2)}";
    }

    public static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
