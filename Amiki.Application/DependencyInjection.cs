using Amiki.Application.Assistant;
using Amiki.Application.Auditing;
using Amiki.Application.Diary;
using Amiki.Application.Entities;
using Amiki.Application.Finance;
using Amiki.Application.People;
using Amiki.Domain.Assistant;
using Amiki.Domain.Diary;
using Amiki.Domain.Finance;
using Microsoft.Extensions.DependencyInjection;

namespace Amiki.Application;

public static class DependencyInjection
{
    /// <summary>The use cases. Their ports (database, model, clock...) come from Infrastructure.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Every kind of item: list / save / delete, with no extra rules unless one is registered below.
        services.AddScoped(typeof(EntityService<>));
        services.AddScoped(typeof(EntityRules<>));
        services.AddScoped<AuditTrail>();

        services.AddScoped<MoneyNames>();
        services.AddScoped<MoneyQueries>();
        services.AddScoped<EntityRules<Transaction>, TransactionRules>();
        services.AddScoped<EntityRules<Transfer>, TransferRules>();
        services.AddScoped<EntityRules<BalanceCheck>, BalanceCheckRules>();
        services.AddScoped<EntityRules<Category>, CategoryRules>();
        services.AddScoped<EntityRules<Plan>, PlanRules>();
        services.AddScoped<EntityRules<DiaryEntry>, DiaryRules>();
        services.AddScoped<EntityRules<AssistantSettings>, AssistantSettingsRules>();

        services.AddScoped<PeopleService>();
        services.AddScoped<MyAccountService>();
        services.AddScoped<SignInService>();

        services.AddScoped<InboxTriageService>();
        services.AddScoped<AssistantTools>();
        services.AddScoped<AssistantProposals>();
        services.AddScoped<BriefingService>();
        services.AddScoped<AssistantChatService>();
        return services;
    }
}
