using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// INotificationSystem 구현체.
    /// Schedule/Cancel, 채널 ON/OFF, 딥링크 라우팅, SaveSystem 스케줄 저장.
    /// </summary>
    public class NotificationSystem : INotificationSystem
    {
        private readonly IRemotePushProvider _remotePushProvider;
        private readonly IDeepLinkHandler _deepLinkHandler;
        private readonly IEventBus _eventBus;
        private readonly ISaveSystem _saveSystem;
        private readonly NotificationConfig _config;

        private readonly Dictionary<string, NotificationData> _scheduledNotifications = new Dictionary<string, NotificationData>();
        private readonly Dictionary<string, bool> _channelStates = new Dictionary<string, bool>();

        private NotificationPermission _permissionState = NotificationPermission.NotDetermined;

        private const int SAVE_SLOT = 0;
        private const string SAVE_KEY_SCHEDULED = "notification_scheduled";
        private const string SAVE_KEY_CHANNELS = "notification_channels";

        public NotificationSystem(IRemotePushProvider remotePushProvider, IDeepLinkHandler deepLinkHandler,
            IEventBus eventBus, ISaveSystem saveSystem, NotificationConfig config)
        {
            _remotePushProvider = remotePushProvider;
            _deepLinkHandler = deepLinkHandler;
            _eventBus = eventBus;
            _saveSystem = saveSystem;
            _config = config;

            LoadChannelStates();
            InitializeChannels();

            Debug.Log("[NotificationSystem] Init started");
        }

        public string Schedule(NotificationData notification)
        {
            if (notification == null)
            {
                Debug.LogError("[NotificationSystem] NotificationData is null");
                return null;
            }

            if (string.IsNullOrEmpty(notification.NotificationId))
            {
                notification.NotificationId = Guid.NewGuid().ToString("N").Substring(0, 10);
            }

            if (!IsChannelEnabled(notification.ChannelId))
            {
                Debug.Log($"[NotificationSystem] Channel disabled: {notification.ChannelId}");
                return null;
            }

            _scheduledNotifications[notification.NotificationId] = notification;
            SaveScheduledData();

            _eventBus?.Publish(new NotificationScheduledEvent
            {
                NotificationId = notification.NotificationId,
                ChannelId = notification.ChannelId,
                DelaySeconds = notification.DelaySeconds
            });

            Debug.Log($"[NotificationSystem] Scheduled: {notification.NotificationId} ({notification.Title}, delay: {notification.DelaySeconds}s)");
            return notification.NotificationId;
        }

        public string ScheduleRepeating(NotificationData notification, float intervalSeconds)
        {
            if (notification == null)
            {
                Debug.LogError("[NotificationSystem] NotificationData is null");
                return null;
            }

            string id = Schedule(notification);

            if (id != null)
            {
                Debug.Log($"[NotificationSystem] Repeating scheduled: {id} (interval: {intervalSeconds}s)");
            }

            return id;
        }

        public void Cancel(string notificationId)
        {
            if (_scheduledNotifications.Remove(notificationId))
            {
                SaveScheduledData();

                _eventBus?.Publish(new NotificationCancelledEvent
                {
                    NotificationId = notificationId
                });

                Debug.Log($"[NotificationSystem] Cancelled: {notificationId}");
            }
        }

        public void CancelAll()
        {
            int count = _scheduledNotifications.Count;
            _scheduledNotifications.Clear();
            SaveScheduledData();

            Debug.Log($"[NotificationSystem] All cancelled ({count})");
        }

        public void SetChannelEnabled(string channelId, bool enabled)
        {
            bool oldState = IsChannelEnabled(channelId);
            _channelStates[channelId] = enabled;
            SaveChannelStates();

            if (oldState != enabled)
            {
                _eventBus?.Publish(new NotificationChannelToggleEvent
                {
                    ChannelId = channelId,
                    Enabled = enabled
                });
            }

            Debug.Log($"[NotificationSystem] Channel {channelId}: {(enabled ? "ON" : "OFF")}");
        }

        public bool IsChannelEnabled(string channelId)
        {
            if (string.IsNullOrEmpty(channelId)) return true;

            if (_channelStates.TryGetValue(channelId, out bool enabled))
            {
                return enabled;
            }

            return true;
        }

        public NotificationPermission GetPermissionState()
        {
            return _permissionState;
        }

        public void RequestPermission(Action<bool> onResult)
        {
            NotificationPermission oldState = _permissionState;
            _permissionState = NotificationPermission.Granted;

            _eventBus?.Publish(new NotificationPermissionChangedEvent
            {
                OldState = oldState,
                NewState = _permissionState
            });

            Debug.Log("[NotificationSystem] Permission granted");
            onResult?.Invoke(true);
        }

        public void OnNotificationReceived(string notificationId, NotificationType type, string title)
        {
            _eventBus?.Publish(new NotificationReceivedEvent
            {
                NotificationId = notificationId,
                Type = type,
                Title = title
            });

            Debug.Log($"[NotificationSystem] Received: {notificationId} ({title})");
        }

        public void OnNotificationOpened(string notificationId, string deepLink)
        {
            _eventBus?.Publish(new NotificationOpenedEvent
            {
                NotificationId = notificationId,
                DeepLink = deepLink
            });

            if (!string.IsNullOrEmpty(deepLink) && _deepLinkHandler != null)
            {
                if (_deepLinkHandler.CanHandle(deepLink))
                {
                    _deepLinkHandler.HandleDeepLink(deepLink);
                }
            }

            Debug.Log($"[NotificationSystem] Opened: {notificationId} (deepLink: {deepLink})");
        }

        private void InitializeChannels()
        {
            if (_config == null) return;

            foreach (NotificationChannel channel in _config.GetChannels())
            {
                if (!_channelStates.ContainsKey(channel.ChannelId))
                {
                    _channelStates[channel.ChannelId] = channel.EnabledByDefault;
                }
            }
        }

        private void LoadChannelStates()
        {
            if (_saveSystem == null) return;

            if (_saveSystem.HasKey(SAVE_SLOT, SAVE_KEY_CHANNELS))
            {
                var data = _saveSystem.Load<Dictionary<string, bool>>(SAVE_SLOT, SAVE_KEY_CHANNELS);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _channelStates[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        private void SaveChannelStates()
        {
            if (_saveSystem == null) return;

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_CHANNELS, _channelStates);
            _saveSystem.WriteToDisk(SAVE_SLOT);
        }

        private void SaveScheduledData()
        {
            if (_saveSystem == null) return;

            _saveSystem.Save(SAVE_SLOT, SAVE_KEY_SCHEDULED, _scheduledNotifications);
            _saveSystem.WriteToDisk(SAVE_SLOT);
        }
    }
}
