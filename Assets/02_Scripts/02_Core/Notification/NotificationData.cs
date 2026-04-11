using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 알림 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class NotificationData
    {
        [SerializeField] private string _notificationId;
        [SerializeField] private string _channelId;
        [SerializeField] private string _title;
        [SerializeField] private string _body;
        [SerializeField] private NotificationType _type;
        [SerializeField] private NotificationImportance _importance;
        [SerializeField] private float _delaySeconds;
        [SerializeField] private string _deepLink;
        [SerializeField] private string _iconName;

        public string NotificationId { get => _notificationId; set => _notificationId = value; }
        public string ChannelId { get => _channelId; set => _channelId = value; }
        public string Title { get => _title; set => _title = value; }
        public string Body { get => _body; set => _body = value; }
        public NotificationType Type { get => _type; set => _type = value; }
        public NotificationImportance Importance { get => _importance; set => _importance = value; }
        public float DelaySeconds { get => _delaySeconds; set => _delaySeconds = value; }
        public string DeepLink { get => _deepLink; set => _deepLink = value; }
        public string IconName { get => _iconName; set => _iconName = value; }
    }
}
