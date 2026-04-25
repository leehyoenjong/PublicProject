using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 하나의 편성 단위. 슬롯 고정 길이(DeckConfig.PartySize) + 리더 + 이름.
    /// 변경은 <see cref="DeckRepository"/> 경유 — 직접 수정 금지.
    /// </summary>
    public class PartyInstance
    {
        private readonly string[] _slots;

        public string DeckId { get; }
        public string Name { get; internal set; }
        public string LeaderInstanceId { get; internal set; }

        public PartyInstance(string deckId, int partySize, string defaultName = null)
        {
            DeckId = deckId;
            Name = string.IsNullOrEmpty(defaultName) ? deckId : defaultName;
            _slots = new string[partySize <= 0 ? 1 : partySize];
        }

        public int PartySize => _slots.Length;
        public IReadOnlyList<string> Slots => _slots;

        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < _slots.Length; i++)
                    if (!string.IsNullOrEmpty(_slots[i])) return false;
                return true;
            }
        }

        internal bool SetSlot(int slot, string instanceId, out string previous)
        {
            previous = null;
            if (slot < 0 || slot >= _slots.Length) return false;
            previous = _slots[slot];
            _slots[slot] = instanceId;
            return true;
        }

        internal int IndexOf(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return -1;
            for (int i = 0; i < _slots.Length; i++)
                if (_slots[i] == instanceId) return i;
            return -1;
        }

        internal void ClearAll()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
            LeaderInstanceId = null;
        }
    }
}
