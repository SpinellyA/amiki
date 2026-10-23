namespace Amiki.Modules.People;

/// <summary>Whether you have a Gemini key saved, and who put it there ("owner" or "self").</summary>
public sealed record MyKeyInfo(string? SetBy)
{
    public bool HasKey => SetBy is not null;
}
