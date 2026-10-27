using Amiki.Application.Assistant;
using Amiki.Application.Data;
using Amiki.Application.People;
using Amiki.Infrastructure.Assistant;
using Amiki.Infrastructure.Data;
using Amiki.Infrastructure.People;
using Amiki.Infrastructure.Security;
using Amiki.Infrastructure.Time;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amiki.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Postgres, Gemini, data protection and the clock, behind the Application's ports.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config, string connectionString)
    {
        services.AddSingleton<TimeProvider>(AmikiTimeProvider.For(config["Amiki:TimeZone"]));

        services.AddDbContext<AmikiDb>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAmikiDb>(sp => sp.GetRequiredService<AmikiDb>());
        services.AddScoped<IPeopleData, PeopleData>();
        services.AddScoped<DevSeed>();
        services.AddSingleton<IDatabaseHealth>(new DatabaseHealth(connectionString));

        // Cookie and API-key encryption keys live in Postgres, so restarts and redeploys don't sign you out.
        services.AddDataProtection().SetApplicationName("Amiki").PersistKeysToDbContext<AmikiDb>();
        services.AddSingleton<ISecretProtector, ApiKeyProtector>();

        services.Configure<GeminiOptions>(config.GetSection("Gemini"));
        services.AddHttpClient<ILanguageModel, GeminiModel>(http => http.Timeout = TimeSpan.FromSeconds(60));
        return services;
    }
}
