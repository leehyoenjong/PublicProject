using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 덱 시스템 Config. 프로젝트가 덱/파티를 사용하지 않으면 기본값(1/1/false/false) 유지.
    /// </summary>
    [CreateAssetMenu(fileName = "DeckConfig", menuName = "PublicFramework/Character/DeckConfig")]
    public class DeckConfig : ScriptableObject
    {
        [Header("덱 / 파티 구성")]
        [SerializeField, Min(1)] private int _maxDecks = 1;
        [SerializeField, Min(1)] private int _partySize = 1;

        [Header("규칙")]
        [SerializeField] private bool _requireLeader = false;
        [SerializeField] private bool _allowDuplicate = false;

        public int MaxDecks => _maxDecks <= 0 ? 1 : _maxDecks;
        public int PartySize => _partySize <= 0 ? 1 : _partySize;
        public bool RequireLeader => _requireLeader;
        public bool AllowDuplicate => _allowDuplicate;
    }
}
