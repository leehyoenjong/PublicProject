using System;

namespace PublicFramework
{
    /// <summary>
    /// 푸시 알림 서비스 인터페이스.
    /// </summary>
    public interface INotificationSystem : IService
    {
        string Schedule(NotificationData notification);
        string ScheduleRepeating(NotificationData notification, float intervalSeconds);
        void Cancel(string notificationId);
        void CancelAll();
        void SetChannelEnabled(string channelId, bool enabled);
        bool IsChannelEnabled(string channelId);
        NotificationPermission GetPermissionState();
        void RequestPermission(Action<bool> onResult);
    }
}
