using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Mail
{
    public class MailSystemTests
    {
        private FakeMailProvider _provider;
        private FakeEventBus _bus;
        private FakeSaveSystem _save;
        private MailConfig _config;
        private MailSystem _system;

        [SetUp]
        public void SetUp()
        {
            _provider = new FakeMailProvider();
            _bus = new FakeEventBus();
            _save = new FakeSaveSystem();
            _config = TestHelpers.MakeMailConfig();
            _system = new MailSystem(_provider, _bus, _save, _config);
        }

        private static MailRewardEntry MakeReward(string id, RewardType type, int amount)
        {
            return new MailRewardEntry { RewardId = id, RewardType = type, Amount = amount };
        }

        private static MailData MakeMail(
            string mailId = null,
            MailType type = MailType.System,
            string title = "Title",
            MailRewardEntry[] rewards = null,
            string expiryTime = null)
        {
            var mail = new MailData
            {
                MailId = mailId,
                MailType = type,
                Title = title,
                Body = "Body",
                SenderName = "Sender",
                ExpiryTime = expiryTime
            };
            if (rewards != null) mail.SetRewards(rewards);
            return mail;
        }

        // ---------- SendMail ----------

        [Test]
        public void SendMail_NewMail_AddsToList()
        {
            _system.SendMail(MakeMail());

            Assert.AreEqual(1, _system.GetAllMails().Count);
        }

        [Test]
        public void SendMail_PublishesReceivedEvent()
        {
            _system.SendMail(MakeMail(title: "T1", rewards: new[] { MakeReward("gold", RewardType.Currency, 100) }));

            var events = _bus.GetPublished<MailReceivedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("T1", events[0].Title);
            Assert.IsTrue(events[0].HasRewards);
        }

        [Test]
        public void SendMail_NullMail_NoOp()
        {
            LogAssert.Expect(LogType.Error, "[MailSystem] MailData is null");

            _system.SendMail(null);

            Assert.AreEqual(0, _system.GetAllMails().Count);
        }

        [Test]
        public void SendMail_AssignsMailIdAndSentTime()
        {
            var mail = MakeMail();
            _system.SendMail(mail);

            Assert.IsFalse(string.IsNullOrEmpty(mail.MailId));
            Assert.IsFalse(string.IsNullOrEmpty(mail.SentTime));
        }

        [Test]
        public void SendMail_AssignsExpiryTime_FromConfig()
        {
            var mail = MakeMail();
            _system.SendMail(mail);

            Assert.IsFalse(string.IsNullOrEmpty(mail.ExpiryTime));
        }

        // ---------- ReadMail ----------

        [Test]
        public void ReadMail_Unread_TransitionsToReadAndPublishesEvent()
        {
            var mail = MakeMail();
            _system.SendMail(mail);
            _bus.Clear();

            _system.ReadMail(mail.MailId);

            Assert.AreEqual(MailState.Read, _system.GetMail(mail.MailId).State);
            Assert.AreEqual(1, _bus.GetPublished<MailReadEvent>().Count);
        }

        [Test]
        public void ReadMail_AlreadyRead_NoOp()
        {
            var mail = MakeMail();
            _system.SendMail(mail);
            _system.ReadMail(mail.MailId);
            _bus.Clear();

            _system.ReadMail(mail.MailId);

            Assert.AreEqual(0, _bus.GetPublished<MailReadEvent>().Count);
        }

        // ---------- ClaimMail ----------

        [Test]
        public void ClaimMail_WithRewards_TransitionsToClaimed()
        {
            var mail = MakeMail(rewards: new[] { MakeReward("gold", RewardType.Currency, 100) });
            _system.SendMail(mail);

            bool ok = _system.ClaimMail(mail.MailId);

            Assert.IsTrue(ok);
            Assert.AreEqual(MailState.Claimed, _system.GetMail(mail.MailId).State);
        }

        [Test]
        public void ClaimMail_PublishesClaimedAndRewardGranted()
        {
            var mail = MakeMail(rewards: new[]
            {
                MakeReward("gold", RewardType.Currency, 100),
                MakeReward("gem", RewardType.Premium, 5)
            });
            _system.SendMail(mail);

            _system.ClaimMail(mail.MailId);

            var claimed = _bus.GetPublished<MailClaimedEvent>();
            Assert.AreEqual(1, claimed.Count);
            Assert.AreEqual(2, claimed[0].RewardCount);

            var rewards = _bus.GetPublished<RewardGrantedEvent>();
            Assert.AreEqual(2, rewards.Count);
            Assert.AreEqual("Mail", rewards[0].Source);
        }

        [Test]
        public void ClaimMail_AlreadyClaimed_Fails()
        {
            var mail = MakeMail(rewards: new[] { MakeReward("gold", RewardType.Currency, 100) });
            _system.SendMail(mail);
            _system.ClaimMail(mail.MailId);

            bool second = _system.ClaimMail(mail.MailId);

            Assert.IsFalse(second);
        }

        [Test]
        public void ClaimMail_Expired_Fails()
        {
            var mail = MakeMail();
            _system.SendMail(mail);
            _system.GetMail(mail.MailId).State = MailState.Expired;

            Assert.IsFalse(_system.ClaimMail(mail.MailId));
        }

        [Test]
        public void ClaimMail_NotFound_Fails()
        {
            Assert.IsFalse(_system.ClaimMail("does-not-exist"));
        }

        [Test]
        public void ClaimMail_ReportsToProvider()
        {
            var mail = MakeMail(rewards: new[] { MakeReward("gold", RewardType.Currency, 100) });
            _system.SendMail(mail);

            _system.ClaimMail(mail.MailId);

            CollectionAssert.Contains(_provider.ClaimedReports, mail.MailId);
        }

        // ---------- ClaimAll ----------

        [Test]
        public void ClaimAll_ClaimsAllUnclaimedWithRewards()
        {
            var m1 = MakeMail(rewards: new[] { MakeReward("a", RewardType.Currency, 1) });
            var m2 = MakeMail(rewards: new[] { MakeReward("b", RewardType.Currency, 2) });
            var m3 = MakeMail(); // 보상 없음
            _system.SendMail(m1);
            _system.SendMail(m2);
            _system.SendMail(m3);

            int claimed = _system.ClaimAll();

            Assert.AreEqual(2, claimed);
            Assert.AreEqual(MailState.Claimed, _system.GetMail(m1.MailId).State);
            Assert.AreEqual(MailState.Claimed, _system.GetMail(m2.MailId).State);
            Assert.AreEqual(MailState.Unread, _system.GetMail(m3.MailId).State);
        }

        [Test]
        public void ClaimAll_TypeFilter_OnlyClaimsMatching()
        {
            var sys = MakeMail(type: MailType.System, rewards: new[] { MakeReward("a", RewardType.Currency, 1) });
            var evt = MakeMail(type: MailType.Event, rewards: new[] { MakeReward("b", RewardType.Currency, 2) });
            _system.SendMail(sys);
            _system.SendMail(evt);

            int claimed = _system.ClaimAll(MailType.Event);

            Assert.AreEqual(1, claimed);
            Assert.AreEqual(MailState.Unread, _system.GetMail(sys.MailId).State);
            Assert.AreEqual(MailState.Claimed, _system.GetMail(evt.MailId).State);
        }

        // ---------- Delete ----------

        [Test]
        public void DeleteMail_Existing_RemovesAndPublishesEvent()
        {
            var mail = MakeMail();
            _system.SendMail(mail);

            bool ok = _system.DeleteMail(mail.MailId);

            Assert.IsTrue(ok);
            Assert.IsNull(_system.GetMail(mail.MailId));
            var events = _bus.GetPublished<MailDeletedEvent>();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(MailDeleteReason.Manual, events[0].Reason);
        }

        [Test]
        public void DeleteClaimedMails_RemovesAllClaimed()
        {
            var m1 = MakeMail(rewards: new[] { MakeReward("a", RewardType.Currency, 1) });
            var m2 = MakeMail();
            _system.SendMail(m1);
            _system.SendMail(m2);
            _system.ClaimMail(m1.MailId);

            int deleted = _system.DeleteClaimedMails();

            Assert.AreEqual(1, deleted);
            Assert.IsNull(_system.GetMail(m1.MailId));
            Assert.IsNotNull(_system.GetMail(m2.MailId));
        }

        // ---------- 조회 ----------

        [Test]
        public void GetUnreadCount_ReturnsCorrect()
        {
            var m1 = MakeMail();
            var m2 = MakeMail();
            _system.SendMail(m1);
            _system.SendMail(m2);
            _system.ReadMail(m1.MailId);

            Assert.AreEqual(1, _system.GetUnreadCount());
        }

        [Test]
        public void GetClaimableCount_OnlyCountsWithRewardsAndNotClaimed()
        {
            _system.SendMail(MakeMail(rewards: new[] { MakeReward("a", RewardType.Currency, 1) }));
            _system.SendMail(MakeMail()); // 보상 없음
            var claimedMail = MakeMail(rewards: new[] { MakeReward("b", RewardType.Currency, 2) });
            _system.SendMail(claimedMail);
            _system.ClaimMail(claimedMail.MailId);

            Assert.AreEqual(1, _system.GetClaimableCount());
        }

        // ---------- 용량 / 만료 / Provider ----------

        [Test]
        public void SendMail_OverflowWithRemovableOldest_ReplacesOldest()
        {
            var smallConfig = TestHelpers.MakeMailConfig(maxMailCount: 2);
            var sys = new MailSystem(_provider, _bus, _save, smallConfig);

            var first = MakeMail();
            sys.SendMail(first);
            sys.ReadMail(first.MailId); // Read + no rewards → 삭제 가능

            var second = MakeMail(rewards: new[] { MakeReward("a", RewardType.Currency, 1) });
            sys.SendMail(second);

            var third = MakeMail();
            sys.SendMail(third);

            Assert.AreEqual(2, sys.GetAllMails().Count);
            Assert.IsNull(sys.GetMail(first.MailId));
            Assert.IsNotNull(sys.GetMail(second.MailId));
            Assert.IsNotNull(sys.GetMail(third.MailId));
        }

        [Test]
        public void SendMail_OverflowAllUnclaimedWithRewards_Rejects()
        {
            var smallConfig = TestHelpers.MakeMailConfig(maxMailCount: 2);
            var sys = new MailSystem(_provider, _bus, _save, smallConfig);
            sys.SendMail(MakeMail(rewards: new[] { MakeReward("a", RewardType.Currency, 1) }));
            sys.SendMail(MakeMail(rewards: new[] { MakeReward("b", RewardType.Currency, 2) }));

            sys.SendMail(MakeMail(rewards: new[] { MakeReward("c", RewardType.Currency, 3) }));

            Assert.AreEqual(2, sys.GetAllMails().Count);
        }

        [Test]
        public void ProcessExpired_PastExpiry_TransitionsAndPublishesEvent()
        {
            var mail = MakeMail();
            _system.SendMail(mail);
            // -2일: KST(+9h) 등 timezone shift 영향을 받지 않도록 충분한 과거로 설정.
            // (MailData.IsExpired 가 DateTime.TryParse 로 Local 변환되어 timezone shift 만큼 차이가 생김)
            mail.ExpiryTime = DateTime.UtcNow.AddDays(-2).ToString("o");
            _bus.Clear();

            _system.ProcessExpired();

            Assert.AreEqual(MailState.Expired, _system.GetMail(mail.MailId).State);
            Assert.AreEqual(1, _bus.GetPublished<MailExpiredEvent>().Count);
        }

        [Test]
        public void FetchFromServer_AddsNewMails()
        {
            int? completedCount = null;
            _provider.NextFetch.Add(MakeMail(mailId: "remote1"));
            _provider.NextFetch.Add(MakeMail(mailId: "remote2"));

            _system.FetchFromServer(c => completedCount = c);

            Assert.AreEqual(2, completedCount);
            Assert.AreEqual(2, _system.GetAllMails().Count);
        }

        // ---------- Save 영속화 ----------

        [Test]
        public void SendMail_PersistsToSaveSystem()
        {
            int beforeSaves = _save.SaveCallCount;
            _system.SendMail(MakeMail());

            Assert.Greater(_save.SaveCallCount, beforeSaves);
            Assert.IsTrue(_save.HasKey(0, "mail_data"));
        }

        [Test]
        public void Constructor_LoadsFromSaveSystem()
        {
            var save = new FakeSaveSystem();
            var data = new MailboxSaveData();
            data.Mails.Add(MakeMail(mailId: "saved1"));
            data.Mails.Add(MakeMail(mailId: "saved2"));
            save.Save(0, "mail_data", data);

            var sys = new MailSystem(_provider, new FakeEventBus(), save, _config);

            Assert.AreEqual(2, sys.GetAllMails().Count);
            Assert.IsNotNull(sys.GetMail("saved1"));
        }
    }
}
