using Amiki.Application.Entities;
using Amiki.Domain.Assistant;

namespace Amiki.Application.Assistant;

/// <summary>What Amiki knows about someone and her personality are the owner's to set.</summary>
public sealed class AssistantSettingsRules : EntityRules<AssistantSettings>
{
    public override void FromMember(AssistantSettings? stored, AssistantSettings incoming) =>
        AssistantSettings.KeepOwnersWords(stored, incoming);
}
