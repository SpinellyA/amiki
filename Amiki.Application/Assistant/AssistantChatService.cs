using Amiki.Application.Data;
using Amiki.Application.People;
using Amiki.Contracts.Assistant;
using Amiki.Domain.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Application.Assistant;

/// <summary>
/// A chat turn with Amiki: her instructions from your settings, the lookups and suggestions
/// you've allowed, and the model. Suggestions come back as proposals for you to confirm.
/// </summary>
public sealed class AssistantChatService(IAmikiDb db, ILanguageModel model, AssistantTools tools, AssistantProposals proposals, ICurrentUser user)
{
    public async Task<ChatReply> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        var allowed = Allowed(settings);
        var looked = new List<string>();
        var proposed = new List<AssistantProposal>();
        var history = request.History.TakeLast(ChatRequest.MaxHistory).ToList();
        var today = request.Now.Date;

        var reply = await model.ChatAsync(AssistantPrompt.Build(settings, request.Now, allowed), history, request.Message.Trim(), allowed,
            async (call, token) =>
            {
                var tool = allowed.FirstOrDefault(t => t.Name == call.Name) ?? throw new ToolException($"There's no tool called {call.Name} available.");
                if (tool.IsProposal)
                {
                    var proposal = await proposals.ProposeAsync(call, today, token);
                    proposed.Add(proposal);
                    return AssistantProposals.Result(proposal);
                }
                if (!looked.Contains(tool.AreaName)) looked.Add(tool.AreaName);
                return await tools.RunAsync(call, allowed, today, token);
            }, settings.Thinking, ct);
        return new ChatReply(reply.Text, looked, proposed, reply.Note);
    }

    public async Task<ContextPreview> PreviewAsync(DateTime now, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        var allowed = Allowed(settings);
        return new ContextPreview(model.Description, AssistantPrompt.Build(settings, now, allowed),
            allowed.Select(t => $"{t.Name}: {t.Description}").ToList());
    }

    /// <summary>A tiny call to check the key and model work.</summary>
    public async Task<string> TestAsync(CancellationToken ct) =>
        (await model.ChatAsync("You are Amiki, a friendly assistant.", [], "Say hi to me in under ten words.", [],
            (_, _) => throw new ToolException("No tools."), ThinkingLevels.Low, ct)).Text;

    // Only the owner edits what she knows about someone, so "remember this" is the owner's alone.
    private IReadOnlyList<ToolDefinition> Allowed(AssistantSettings settings) =>
        [.. tools.For(settings), .. proposals.For(settings, canRemember: user.IsOwner)];

    // Saved settings, or the defaults if nothing was changed yet.
    private async Task<AssistantSettings> SettingsAsync(CancellationToken ct) =>
        await db.AssistantSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new AssistantSettings();
}
