using Amiki.Api.Data;
using Amiki.Modules.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Amiki.Api.Assistant;

/// <summary>A chat turn with Amiki: her instructions from your settings, the tools you've allowed, and the model.</summary>
public sealed class AssistantChatService(AmikiDb db, ILanguageModel model, AssistantTools tools)
{
    public async Task<ChatReply> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        var allowed = tools.For(settings);
        var looked = new List<string>();
        var history = request.History.TakeLast(ChatRequest.MaxHistory).ToList();

        var reply = await model.ChatAsync(AssistantPrompt.Build(settings, request.Now, allowed), history, request.Message.Trim(), allowed,
            async (call, token) =>
            {
                var area = allowed.FirstOrDefault(t => t.Name == call.Name)?.Area;
                if (area is not null && !looked.Contains(area)) looked.Add(area);
                return await tools.RunAsync(call, allowed, request.Now.Date, token);
            }, ct);
        return new ChatReply(reply, looked);
    }

    public async Task<ContextPreview> PreviewAsync(DateTime now, CancellationToken ct)
    {
        var settings = await SettingsAsync(ct);
        var allowed = tools.For(settings);
        return new ContextPreview(model.Description, AssistantPrompt.Build(settings, now, allowed),
            allowed.Select(t => $"{t.Name}: {t.Description}").ToList());
    }

    /// <summary>A tiny call to check the key and model work.</summary>
    public Task<string> TestAsync(CancellationToken ct) =>
        model.ChatAsync("You are Amiki, a friendly assistant.", [], "Say hi to me in under ten words.", [], (_, _) => throw new ToolException("No tools."), ct);

    // Saved settings, or the defaults if you haven't changed anything yet.
    private async Task<AssistantSettings> SettingsAsync(CancellationToken ct) =>
        await db.AssistantSettings.AsNoTracking().FirstOrDefaultAsync(ct) ?? new AssistantSettings();
}
