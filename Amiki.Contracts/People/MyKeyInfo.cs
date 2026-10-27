using Amiki.Domain.People;

namespace Amiki.Contracts.People;

/// <summary>Whether you have a Gemini key saved, and who put it there.</summary>
public sealed record MyKeyInfo(KeySource? SetBy)
{
    public bool HasKey => SetBy is not null;
}
