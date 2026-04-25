using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 편성/덱 저장소. 프로젝트가 덱 기능을 쓰지 않을 때도 MaxDecks=1 / PartySize=1 로 무해하게 동작한다.
    /// </summary>
    public interface IDeckRepository : IService
    {
        int MaxDecks { get; }
        int PartySize { get; }
        IReadOnlyList<PartyInstance> Decks { get; }
        PartyInstance Get(string deckId);

        DeckResult SetMember(string deckId, int slot, string instanceId);
        DeckResult RemoveMember(string deckId, int slot);
        DeckResult SetLeader(string deckId, string instanceId);
        DeckResult Rename(string deckId, string name);
        DeckResult Clear(string deckId);
    }
}
