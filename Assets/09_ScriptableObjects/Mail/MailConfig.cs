using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ScriptableObject 기반 우편함 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "MailConfig", menuName = "PublicFramework/Mail/MailConfig")]
    public class MailConfig : ScriptableObject
    {
        [Header("보관 제한")]
        [SerializeField] private int _maxMailCount = 100;
        [SerializeField] private float _nearFullThreshold = 0.9f;

        [Header("만료 설정")]
        [SerializeField] private int _defaultExpiryDays = 30;
        [SerializeField] private int _expiredAutoDeleteDays = 3;
        [SerializeField] private float _expiredCheckInterval = 60f;

        public int MaxMailCount => _maxMailCount;
        public float NearFullThreshold => _nearFullThreshold;
        public int DefaultExpiryDays => _defaultExpiryDays;
        public int ExpiredAutoDeleteDays => _expiredAutoDeleteDays;
        public float ExpiredCheckInterval => _expiredCheckInterval;
    }
}
