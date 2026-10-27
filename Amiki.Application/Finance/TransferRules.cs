using Amiki.Application.Entities;
using Amiki.Domain.Finance;

namespace Amiki.Application.Finance;

/// <summary>Both accounts of a transfer must be real ones. Every change is audited.</summary>
public sealed class TransferRules(MoneyNames names) : EntityRules<Transfer>
{
    public override async Task<string?> CheckAsync(Transfer incoming, CancellationToken ct) =>
        await names.AccountProblemAsync(incoming.From, ct) ?? await names.AccountProblemAsync(incoming.To, ct);

    public override string AuditAs => "transfers";

    public override string Describe(Transfer item) => item.Describe();
}
