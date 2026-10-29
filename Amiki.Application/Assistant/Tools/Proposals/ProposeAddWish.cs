using Amiki.Domain.Assistant;
using Amiki.Domain.Wishlist;

namespace Amiki.Application.Assistant.Tools.Proposals;

public sealed class ProposeAddWish : IProposalTool
{
    public ToolDefinition Definition { get; } = ToolDefinition.Create("propose_add_wish",
        "Suggest adding something the user wants to buy to their wishlist (e.g. 'I really want a new mouse, around 1,200'). Not for things they already bought; log those as money spent.",
        AssistantArea.Money, """
        {"type":"object","properties":{
          "name":{"type":"string","description":"What they want, short, e.g. 'Wireless mouse'"},
          "price":{"type":"number","description":"Price in pesos, if they said or it's clear; omit if unknown"}},
         "required":["name"]}
        """);

    public Task<AssistantProposal> ProposeAsync(ToolArguments args, DateTime today, CancellationToken ct)
    {
        var price = Math.Round(args.Amount("price") ?? 0, 2);
        if (price is < 0 or > WishItem.MaxPrice) throw new ToolException("'price' must be a real price in pesos.");
        return Task.FromResult(new AssistantProposal(Guid.NewGuid(), ProposalKind.AddWish,
            args.RequiredText("name", WishItem.MaxName), Amount: price));
    }
}
