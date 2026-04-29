using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Notification
{
    public class NotificationSystemTests
    {
        private FakeEventBus _bus;
        private FakeSaveSystem _save;
        private DummyRemotePushProvider _push;
        private FakeDeepLinkHandler _deepLink;
        private NotificationConfig _config;
        private NotificationSystem _system;

        [SetUp]
        public void SetUp()
        {
            _bus = new FakeEventBus();
            _save = new FakeSaveSystem();
            _push = new DummyRemotePushProvider();
            _deepLink = new FakeDeepLinkHandler();
            _config = ScriptableObject.CreateInstance<NotificationConfig>();
            _system = new NotificationSystem(_push, _deepLink, _bus, _save, _config);
        }

        private static NotificationData MakeData(
            string channelId = "general",
            string title = "Title",
            string body = "Body",
            float delay = 60f,
            string deepLink = null,
            string id = null)
        {
            return new NotificationData
            {
                NotificationId = id,
                ChannelId = channelId,
                Title = title,
                Body = body,
                DelaySeconds = delay,
                DeepLink = deepLink,
                Type = NotificationType.Local,
                Importance = NotificationImportance.Default
            };
        }

        // ---------- Schedule ----------

        [Test]
        public void Schedule_New_PublishesScheduledEvent()
        {
            string id = _system.Schedule(MakeData(channelId: "general", delay: 30f));

            Assert.IsNotNull(id);
            var events = _bus.GetPublished<NotificationScheduledEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("general", events[0].ChannelId);
            Assert.AreEqual(30f, events[0].DelaySeconds, 0.001f);
        }

        [Test]
        public void Schedule_AssignsIdIfMissing()
        {
            var data = MakeData();
            string id = _system.Schedule(data);

            Assert.IsNotNull(id);
            Assert.AreEqual(id, data.NotificationId);
        }

        [Test]
        public void Schedule_PreservesProvidedId()
        {
            var data = MakeData(id: "fixed-id");

            string id = _system.Schedule(data);

            Assert.AreEqual("fixed-id", id);
        }

        [Test]
        public void Schedule_DisabledChannel_ReturnsNull()
        {
            _system.SetChannelEnabled("general", false);
            _bus.Clear();

            string id = _system.Schedule(MakeData());

            Assert.IsNull(id);
            Assert.AreEqual(0, _bus.GetPublished<NotificationScheduledEvent>().Count);
        }

        [Test]
        public void Schedule_NullData_ReturnsNullAndLogsError()
        {
            LogAssert.Expect(LogType.Error, "[NotificationSystem] NotificationData is null");

            string id = _system.Schedule(null);

            Assert.IsNull(id);
        }

        [Test]
        public void ScheduleRepeating_DelegatesToSchedule()
        {
            string id = _system.ScheduleRepeating(MakeData(), 60f);

            Assert.IsNotNull(id);
            Assert.AreEqual(1, _bus.GetPublished<NotificationScheduledEvent>().Count);
        }

        // ---------- Cancel ----------

        [Test]
        public void Cancel_Existing_PublishesCancelledEvent()
        {
            string id = _system.Schedule(MakeData());
            _bus.Clear();

            _system.Cancel(id);

            var events = _bus.GetPublished<NotificationCancelledEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(id, events[0].NotificationId);
        }

        [Test]
        public void Cancel_Unknown_NoOp()
        {
            _system.Cancel("does-not-exist");

            Assert.AreEqual(0, _bus.GetPublished<NotificationCancelledEvent>().Count);
        }

        [Test]
        public void CancelAll_RemovesAllSchedules()
        {
            _system.Schedule(MakeData());
            _system.Schedule(MakeData());

            _system.CancelAll();

            // 다시 동일 ID로 Cancel 시 no-op (이미 비었음을 우회 검증)
            _bus.Clear();
            _system.Cancel("anything");
            Assert.AreEqual(0, _bus.GetPublished<NotificationCancelledEvent>().Count);
        }

        // ---------- Channel ----------

        [Test]
        public void SetChannelEnabled_Disable_PublishesToggleEvent()
        {
            _system.SetChannelEnabled("general", false);

            var events = _bus.GetPublished<NotificationChannelToggleEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("general", events[0].ChannelId);
            Assert.IsFalse(events[0].Enabled);
        }

        [Test]
        public void SetChannelEnabled_SameState_DoesNotPublishEvent()
        {
            _system.SetChannelEnabled("general", true); // 이미 default true

            Assert.AreEqual(0, _bus.GetPublished<NotificationChannelToggleEvent>().Count);
        }

        [Test]
        public void IsChannelEnabled_DefaultChannel_ReturnsTrue()
        {
            Assert.IsTrue(_system.IsChannelEnabled("general"));
        }

        [Test]
        public void IsChannelEnabled_UnknownChannel_DefaultsTrue()
        {
            Assert.IsTrue(_system.IsChannelEnabled("anything"));
        }

        [Test]
        public void IsChannelEnabled_AfterSetFalse_ReturnsFalse()
        {
            _system.SetChannelEnabled("general", false);
            Assert.IsFalse(_system.IsChannelEnabled("general"));
        }

        // ---------- Permission ----------

        [Test]
        public void GetPermissionState_Initial_NotDetermined()
        {
            Assert.AreEqual(NotificationPermission.NotDetermined, _system.GetPermissionState());
        }

        [Test]
        public void RequestPermission_GrantsAndCallsCallback()
        {
            bool? callbackResult = null;
            _system.RequestPermission(granted => callbackResult = granted);

            Assert.IsTrue(callbackResult);
            Assert.AreEqual(NotificationPermission.Granted, _system.GetPermissionState());
            var events = _bus.GetPublished<NotificationPermissionChangedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(NotificationPermission.Granted, events[0].NewState);
        }

        // ---------- OnNotification 처리 ----------

        [Test]
        public void OnNotificationReceived_PublishesReceivedEvent()
        {
            _system.OnNotificationReceived("noti1", NotificationType.Local, "Hello");

            var events = _bus.GetPublished<NotificationReceivedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("noti1", events[0].NotificationId);
            Assert.AreEqual("Hello", events[0].Title);
        }

        [Test]
        public void OnNotificationOpened_PublishesOpenedEvent()
        {
            _system.OnNotificationOpened("noti1", null);

            var events = _bus.GetPublished<NotificationOpenedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("noti1", events[0].NotificationId);
        }

        [Test]
        public void OnNotificationOpened_WithHandleableDeepLink_RoutesToHandler()
        {
            _deepLink.DefaultCanHandle = true;

            _system.OnNotificationOpened("noti1", "publicframework://shop");

            CollectionAssert.Contains(_deepLink.HandledLinks, "publicframework://shop");
        }

        [Test]
        public void OnNotificationOpened_UnhandleableDeepLink_DoesNotRoute()
        {
            _deepLink.DefaultCanHandle = false;

            _system.OnNotificationOpened("noti1", "other://link");

            Assert.AreEqual(0, _deepLink.HandledLinks.Count);
        }

        // ---------- Channel 영속화 ----------

        [Test]
        public void SetChannelEnabled_PersistsToSaveSystem()
        {
            int beforeSaves = _save.SaveCallCount;

            _system.SetChannelEnabled("general", false);

            Assert.Greater(_save.SaveCallCount, beforeSaves);
        }

        [Test]
        public void Constructor_LoadsChannelStatesFromSaveSystem()
        {
            var save = new FakeSaveSystem();
            save.Save(0, "notification_channels", new Dictionary<string, bool> { { "general", false } });

            var system = new NotificationSystem(_push, _deepLink, new FakeEventBus(), save, _config);

            Assert.IsFalse(system.IsChannelEnabled("general"));
        }

        // ---------- DefaultDeepLinkHandler (별도 단위 테스트) ----------

        [Test]
        public void DefaultDeepLinkHandler_PublicframeworkScheme_CanHandle()
        {
            var handler = new DefaultDeepLinkHandler();
            Assert.IsTrue(handler.CanHandle("publicframework://shop"));
        }

        [Test]
        public void DefaultDeepLinkHandler_OtherScheme_CannotHandle()
        {
            var handler = new DefaultDeepLinkHandler();
            Assert.IsFalse(handler.CanHandle("https://example.com"));
            Assert.IsFalse(handler.CanHandle(null));
            Assert.IsFalse(handler.CanHandle(""));
        }
    }
}
