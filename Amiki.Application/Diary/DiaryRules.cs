using Amiki.Application.Entities;
using Amiki.Domain.Diary;

namespace Amiki.Application.Diary;

/// <summary>The diary is yours alone: not even the owner sees it when viewing your Amiki.</summary>
public sealed class DiaryRules : EntityRules<DiaryEntry>
{
    public override bool PrivateFromOwner => true;
}
