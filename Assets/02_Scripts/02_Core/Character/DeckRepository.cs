using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 기본 DeckRepository 구현. DeckConfig 로부터 덱 수/슬롯 수를 받아 Initialize 시점에 PartyInstance 배열을 할당한다.
    /// 이벤트 버스는 선택 — null 허용.
    /// </summary>
    public class DeckRepository : IDeckRepository
    {
        private readonly List<PartyInstance> _decks = new();
        private readonly DeckConfig _config;
        private readonly IEventBus _eventBus;

        public DeckRepository(DeckConfig config, IEventBus eventBus = null)
        {
            _config = config;
            _eventBus = eventBus;
            Initialize();
        }

        private void Initialize()
        {
            for (int i = 0; i < _config.MaxDecks; i++)
            {
                var deckId = $"deck_{i + 1}";
                _decks.Add(new PartyInstance(deckId, _config.PartySize));
            }
        }

        public int MaxDecks => _config.MaxDecks;
        public int PartySize => _config.PartySize;
        public IReadOnlyList<PartyInstance> Decks => _decks;

        public PartyInstance Get(string deckId)
        {
            for (int i = 0; i < _decks.Count; i++)
                if (_decks[i].DeckId == deckId) return _decks[i];
            return null;
        }

        public DeckResult SetMember(string deckId, int slot, string instanceId)
        {
            var deck = Get(deckId);
            if (deck == null) return DeckResult.DeckNotFound;
            if (slot < 0 || slot >= deck.PartySize) return DeckResult.SlotOutOfRange;
            if (string.IsNullOrEmpty(instanceId)) return DeckResult.EmptyInstance;

            if (!_config.AllowDuplicate)
            {
                int existing = deck.IndexOf(instanceId);
                if (existing >= 0 && existing != slot) return DeckResult.DuplicateNotAllowed;
            }

            deck.SetSlot(slot, instanceId, out var previous);
            PublishMemberChanged(deckId, slot, previous, instanceId);
            return DeckResult.Success;
        }

        public DeckResult RemoveMember(string deckId, int slot)
        {
            var deck = Get(deckId);
            if (deck == null) return DeckResult.DeckNotFound;
            if (slot < 0 || slot >= deck.PartySize) return DeckResult.SlotOutOfRange;

            deck.SetSlot(slot, null, out var previous);
            if (string.IsNullOrEmpty(previous)) return DeckResult.MemberNotFound;

            if (deck.LeaderInstanceId == previous)
            {
                var oldLeader = deck.LeaderInstanceId;
                deck.LeaderInstanceId = null;
                PublishLeaderChanged(deckId, oldLeader, null);
            }
            PublishMemberChanged(deckId, slot, previous, null);
            return DeckResult.Success;
        }

        public DeckResult SetLeader(string deckId, string instanceId)
        {
            var deck = Get(deckId);
            if (deck == null) return DeckResult.DeckNotFound;

            if (string.IsNullOrEmpty(instanceId))
            {
                var oldLeader = deck.LeaderInstanceId;
                if (string.IsNullOrEmpty(oldLeader)) return DeckResult.MemberNotFound;
                deck.LeaderInstanceId = null;
                PublishLeaderChanged(deckId, oldLeader, null);
                return DeckResult.Success;
            }

            if (deck.IndexOf(instanceId) < 0) return DeckResult.MemberNotFound;

            var old = deck.LeaderInstanceId;
            deck.LeaderInstanceId = instanceId;
            PublishLeaderChanged(deckId, old, instanceId);
            return DeckResult.Success;
        }

        public DeckResult Rename(string deckId, string name)
        {
            var deck = Get(deckId);
            if (deck == null) return DeckResult.DeckNotFound;
            if (string.IsNullOrWhiteSpace(name)) return DeckResult.InvalidName;

            var old = deck.Name;
            deck.Name = name;
            _eventBus?.Publish(new DeckRenamedEvent { DeckId = deckId, OldName = old, NewName = name });
            return DeckResult.Success;
        }

        public DeckResult Clear(string deckId)
        {
            var deck = Get(deckId);
            if (deck == null) return DeckResult.DeckNotFound;
            deck.ClearAll();
            _eventBus?.Publish(new DeckClearedEvent { DeckId = deckId });
            return DeckResult.Success;
        }

        private void PublishMemberChanged(string deckId, int slot, string oldId, string newId)
        {
            _eventBus?.Publish(new DeckMemberChangedEvent
            {
                DeckId = deckId,
                Slot = slot,
                OldInstanceId = oldId,
                NewInstanceId = newId
            });
        }

        private void PublishLeaderChanged(string deckId, string oldId, string newId)
        {
            _eventBus?.Publish(new DeckLeaderChangedEvent
            {
                DeckId = deckId,
                OldLeaderId = oldId,
                NewLeaderId = newId
            });
        }
    }
}
