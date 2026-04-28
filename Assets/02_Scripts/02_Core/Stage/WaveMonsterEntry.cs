using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 웨이브 안 몬스터 1종 스폰 정보. WaveData._monsters 인라인 배열.
    /// </summary>
    [Serializable]
    public class WaveMonsterEntry
    {
        [SerializeField] private string _monsterMID;
        [SerializeField] private int _count;
        [SerializeField] private float _spawnTiming;
        [SerializeField] private SpawnPattern _spawnPattern;

        public string MonsterMID => _monsterMID;
        public int Count => _count;
        public float SpawnTiming => _spawnTiming;
        public SpawnPattern SpawnPattern => _spawnPattern;

        public WaveMonsterEntry() { }

        public WaveMonsterEntry(string monsterMID, int count, float spawnTiming, SpawnPattern spawnPattern)
        {
            _monsterMID = monsterMID;
            _count = count;
            _spawnTiming = spawnTiming;
            _spawnPattern = spawnPattern;
        }
    }
}
