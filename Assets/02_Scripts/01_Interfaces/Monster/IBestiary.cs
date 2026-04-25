using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>도감. 처음 처치한 몬스터 MID 를 기록하고 열람한다.</summary>
    public interface IBestiary
    {
        bool IsEntered(string monsterMID);
        bool Register(string monsterMID, DateTime utcNow);
        IReadOnlyCollection<BestiaryEntry> Entries { get; }
    }

    /// <summary>도감 단일 항목.</summary>
    public struct BestiaryEntry
    {
        public string MonsterMID;
        public DateTime FirstSeenUtc;
    }
}
