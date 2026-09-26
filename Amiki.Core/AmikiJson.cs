using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amiki.Core;

/// <summary>
/// JSON settings shared by the API, the client, and the database's jsonb columns, so all three
/// read and write the same shapes.
/// </summary>
public static class AmikiJson
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Configure(options);
        return options;
    }

    public static void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new WallClockDateTimeConverter());
        // "Expense" rather than 0: readable, and reordering an enum can't silently change stored data.
        options.Converters.Add(new JsonStringEnumConverter());
    }
}
