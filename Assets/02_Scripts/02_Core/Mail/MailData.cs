using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 우편 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class MailData
    {
        [SerializeField] private string _mailId;
        [SerializeField] private MailType _mailType;
        [SerializeField] private MailState _state;
        [SerializeField] private string _senderName;
        [SerializeField] private string _title;
        [SerializeField] private string _body;
        [SerializeField] private MailRewardEntry[] _rewards;
        [SerializeField] private string _sentTime;
        [SerializeField] private string _expiryTime;

        public string MailId { get => _mailId; set => _mailId = value; }
        public MailType MailType { get => _mailType; set => _mailType = value; }
        public MailState State { get => _state; set => _state = value; }
        public string SenderName { get => _senderName; set => _senderName = value; }
        public string Title { get => _title; set => _title = value; }
        public string Body { get => _body; set => _body = value; }
        public IReadOnlyList<MailRewardEntry> Rewards => _rewards;
        public void SetRewards(MailRewardEntry[] rewards) { _rewards = rewards; }
        public string SentTime { get => _sentTime; set => _sentTime = value; }
        public string ExpiryTime { get => _expiryTime; set => _expiryTime = value; }

        public bool HasRewards => _rewards != null && _rewards.Length > 0;

        public bool IsExpired
        {
            get
            {
                if (string.IsNullOrEmpty(_expiryTime)) return false;
                // RoundtripKind 로 파싱해 Kind=Utc 보존. 그렇지 않으면 Local 변환되어
                // timezone offset 만큼 비교 결과가 어긋남.
                return DateTime.TryParse(_expiryTime, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime expiry) && DateTime.UtcNow > expiry;
            }
        }
    }
}
