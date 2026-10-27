using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Amiki.Infrastructure.Data;

/// <summary>Brings the database up to date at startup, so a deploy is a single step.</summary>
public static class DatabaseStartup
{
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken ct = default) =>
        await services.GetRequiredService<AmikiDb>().Database.MigrateAsync(ct);
}
