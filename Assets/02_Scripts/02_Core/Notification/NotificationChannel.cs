using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 알림 채널 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class NotificationChannel
    {
        [SerializeField] private string _channelId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private NotificationImportance _importance;
        [SerializeField] private bool _enabledByDefault;

        public string ChannelId { get => _channelId; set => _channelId = value; }
        public string DisplayName { get => _displayName; set => _displayName = value; }
        public string Description { get => _description; set => _description = value; }
        public NotificationImportance Importance { get => _importance; set => _importance = value; }
        public bool EnabledByDefault { get => _enabledByDefault; set => _enabledByDefault = value; }
    }
}
