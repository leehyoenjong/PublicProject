using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace PublicFramework.Tests.Pause
{
    /// <summary>
    /// PauseService 시간 제어/상태/통지 검증.
    /// Time.timeScale 을 건드리므로 SetUp/TearDown 에서 1 로 복원한다.
    /// </summary>
    public class PauseServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void Default_IsPaused_False()
        {
            Assert.IsFalse(new PauseService().IsPaused);
        }

        [Test]
        public void Pause_SetsTimeScaleZero_AndIsPaused()
        {
            var svc = new PauseService();
            svc.Pause();
            Assert.IsTrue(svc.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
        }

        [Test]
        public void Resume_RestoresTimeScale_AndClearsPaused()
        {
            var svc = new PauseService();
            svc.Pause();
            svc.Resume();
            Assert.IsFalse(svc.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void Resume_PreservesCustomTimeScale()
        {
            Time.timeScale = 0.5f; // 슬로모션 환경
            var svc = new PauseService();
            svc.Pause();
            Assert.AreEqual(0f, Time.timeScale);
            svc.Resume();
            Assert.AreEqual(0.5f, Time.timeScale);
        }

        [Test]
        public void Pause_WhenAlreadyZeroTimeScale_RestoresToOne()
        {
            Time.timeScale = 0f; // 다른 이유로 이미 멈춤
            var svc = new PauseService();
            svc.Pause();
            svc.Resume();
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void DoublePause_Ignored_KeepsResumeBaseline()
        {
            Time.timeScale = 0.5f;
            var svc = new PauseService();
            svc.Pause();           // 복원 기준 0.5 기억
            Time.timeScale = 0f;   // 외부에서 또 건드려도
            svc.Pause();           // 두 번째 Pause 는 무시 — 기준 갱신 안 됨
            svc.Resume();
            Assert.AreEqual(0.5f, Time.timeScale);
        }

        [Test]
        public void Resume_WhenNotPaused_Ignored()
        {
            var svc = new PauseService();
            svc.Resume(); // 멈춘 적 없음
            Assert.IsFalse(svc.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void Toggle_AlternatesState()
        {
            var svc = new PauseService();
            svc.Toggle();
            Assert.IsTrue(svc.IsPaused);
            svc.Toggle();
            Assert.IsFalse(svc.IsPaused);
        }

        [Test]
        public void PauseChanged_FiresWithCorrectValue()
        {
            var svc = new PauseService();
            var received = new List<bool>();
            svc.PauseChanged += received.Add;

            svc.Pause();
            svc.Resume();

            Assert.AreEqual(2, received.Count);
            Assert.IsTrue(received[0]);
            Assert.IsFalse(received[1]);
        }

        [Test]
        public void EventBus_PublishesPauseChangedEvent_WhenInjected()
        {
            var bus = new EventBus();
            var svc = new PauseService(bus);
            var received = new List<bool>();
            bus.Subscribe<PauseChangedEvent>(e => received.Add(e.IsPaused));

            svc.Pause();
            svc.Resume();

            Assert.AreEqual(2, received.Count);
            Assert.IsTrue(received[0]);
            Assert.IsFalse(received[1]);
        }
    }
}
