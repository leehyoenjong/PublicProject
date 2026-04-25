using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>인메모리 도감. 영속화는 별도 Repository 가 담당하면 됨.</summary>
    public class Bestiary : IBestiary
    {
        private readonly Dictionary<string, BestiaryEntry> _entries = new();

        public IReadOnlyCollection<BestiaryEntry> Entries => _entries.Values;

        public bool IsEntered(string monsterMID) =>
            !string.IsNullOrEmpty(monsterMID) && _entries.ContainsKey(monsterMID);

        public bool Register(string monsterMID, DateTime utcNow)
        {
            if (string.IsNullOrEmpty(monsterMID)) return false;
            if (_entries.ContainsKey(monsterMID)) return false;
            _entries[monsterMID] = new BestiaryEntry { MonsterMID = monsterMID, FirstSeenUtc = utcNow };
            return true;
        }
    }
}
