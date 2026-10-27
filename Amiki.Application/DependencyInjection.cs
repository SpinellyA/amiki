using Amiki.Application.Assistant;
using Amiki.Application.Assistant.Tools;
using Amiki.Application.Assistant.Tools.Lookups;
using Amiki.Application.Assistant.Tools.Proposals;
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

        services.AddScoped<MoneyBook>();

        services.AddScoped<InboxTriageService>();
        services.AddScoped<AssistantChatService>();
        services.AddScoped<BriefingService>();
        services.AddSingleton<BriefingGate>();

        // What Amiki can look up. Add a lookup: write an ILookupTool and list it here.
        services.AddScoped<AssistantTools>();
        services.AddScoped<ILookupTool, ListTasksTool>();
        services.AddScoped<ILookupTool, ListIdeasTool>();
        services.AddScoped<ILookupTool, AccountBalancesTool>();
        services.AddScoped<ILookupTool, MoneySummaryTool>();
        services.AddScoped<ILookupTool, SpendingByPeriodTool>();
        services.AddScoped<ILookupTool, ListTransactionsTool>();
        services.AddScoped<ILookupTool, ListInboxTool>();
        services.AddScoped<ILookupTool, WeeklyNotesTool>();

        // What she can suggest. Add one: write an IProposalTool and list it here.
        services.AddScoped<AssistantProposals>();
        services.AddScoped<IProposalTool, ProposeAddTask>();
        services.AddScoped<IProposalTool, ProposeCompleteTask>();
        services.AddScoped<IProposalTool, ProposeLogMoney>();
        services.AddScoped<IProposalTool, ProposeAddIdea>();
        services.AddScoped<IProposalTool, ProposeInboxNote>();
        services.AddScoped<IProposalTool, ProposeRemember>();
        return services;
    }
}
