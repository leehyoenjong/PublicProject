using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ScriptableObject 기반 푸시 알림 설정.
    /// </summary>
    [CreateAssetMenu(fileName = "NotificationConfig", menuName = "PublicFramework/Notification/NotificationConfig")]
    public class NotificationConfig : ScriptableObject
    {
        [Header("채널 설정")]
        [SerializeField] private NotificationChannel[] _channels =
        {
            new NotificationChannel
            {
                ChannelId = "general", DisplayName = "일반", Description = "일반 알림",
                Importance = NotificationImportance.Default, EnabledByDefault = true
            },
            new NotificationChannel
            {
                ChannelId = "event", DisplayName = "이벤트", Description = "이벤트 알림",
                Importance = NotificationImportance.High, EnabledByDefault = true
            },
            new NotificationChannel
            {
                ChannelId = "reward", DisplayName = "보상", Description = "보상 수령 알림",
                Importance = NotificationImportance.Default, EnabledByDefault = true
            }
        };

        [Header("기본 설정")]
        [SerializeField] private string _deepLinkScheme = "publicframework://";

        public NotificationChannel[] GetChannels()
        {
            var copy = new NotificationChannel[_channels.Length];
            Array.Copy(_channels, copy, _channels.Length);
            return copy;
        }

        public string DeepLinkScheme => _deepLinkScheme;
    }
}
