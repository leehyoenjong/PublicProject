using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PublicFramework.Tests.Sound
{
    /// <summary>
    /// EditMode 부분 커버 — SoundManager 의 Coroutine 진입 흐름(BGM Crossfade / SFX TrackEnd / Fade)은
    /// inactive runner 에서 ArgumentException 을 던지므로 PlayMode 로 위임.
    /// 본 테스트는 Volume Clamp / Mute 정책 / RegisterSound null id / 미등록 ID 에러만 검증.
    /// </summary>
    public class SoundManagerTests
    {
        private GameObject _runnerGo;
        private GameObject _parentGo;
        private SoundManager _manager;

        [SetUp]
        public void SetUp()
        {
            _runnerGo = new GameObject("SoundRunner");
            _runnerGo.SetActive(false); // Coroutine 시작 거부 → 안전 영역만 검증
            BootScene runner = _runnerGo.AddComponent<BootScene>();
            _parentGo = new GameObject("SoundParent");
            _manager = new SoundManager(runner, _parentGo.transform, sfxPoolSize: 2);
        }

        [TearDown]
        public void TearDown()
        {
            if (_runnerGo != null) Object.DestroyImmediate(_runnerGo);
            if (_parentGo != null) Object.DestroyImmediate(_parentGo);
        }

        // ---------- Volume ----------

        [Test]
        public void Volumes_Default_AllOne()
        {
            Assert.AreEqual(1f, _manager.MasterVolume);
            Assert.AreEqual(1f, _manager.BGMVolume);
            Assert.AreEqual(1f, _manager.SFXVolume);
            Assert.AreEqual(1f, _manager.VoiceVolume);
        }

        [Test]
        public void SetMasterVolume_AboveOne_ClampsToOne()
        {
            _manager.SetMasterVolume(2.5f);
            Assert.AreEqual(1f, _manager.MasterVolume);
        }

        [Test]
        public void SetMasterVolume_Negative_ClampsToZero()
        {
            _manager.SetMasterVolume(-3f);
            Assert.AreEqual(0f, _manager.MasterVolume);
        }

        [Test]
        public void SetBGMVolume_StoresInRange()
        {
            _manager.SetBGMVolume(0.4f);
            Assert.AreEqual(0.4f, _manager.BGMVolume, 0.001f);
        }

        [Test]
        public void SetSFXVolume_StoresInRange()
        {
            _manager.SetSFXVolume(0.7f);
            Assert.AreEqual(0.7f, _manager.SFXVolume, 0.001f);
        }

        [Test]
        public void SetVoiceVolume_StoresInRange()
        {
            _manager.SetVoiceVolume(0.5f);
            Assert.AreEqual(0.5f, _manager.VoiceVolume, 0.001f);
        }

        // ---------- Mute ----------

        [Test]
        public void IsMuted_Default_AllFalse()
        {
            Assert.IsFalse(_manager.IsMuted(SoundChannel.BGM));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.SFX));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.Voice));
        }

        [Test]
        public void SetMute_BGM_OnlyAffectsBGM()
        {
            _manager.SetMute(SoundChannel.BGM, true);

            Assert.IsTrue(_manager.IsMuted(SoundChannel.BGM));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.SFX));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.Voice));
        }

        [Test]
        public void SetMute_SFX_OnlyAffectsSFX()
        {
            _manager.SetMute(SoundChannel.SFX, true);

            Assert.IsTrue(_manager.IsMuted(SoundChannel.SFX));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.BGM));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.Voice));
        }

        [Test]
        public void SetMute_Voice_OnlyAffectsVoice()
        {
            _manager.SetMute(SoundChannel.Voice, true);

            Assert.IsTrue(_manager.IsMuted(SoundChannel.Voice));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.BGM));
            Assert.IsFalse(_manager.IsMuted(SoundChannel.SFX));
        }

        [Test]
        public void SetMute_Toggle_RestoresFalse()
        {
            _manager.SetMute(SoundChannel.SFX, true);
            _manager.SetMute(SoundChannel.SFX, false);

            Assert.IsFalse(_manager.IsMuted(SoundChannel.SFX));
        }

        // ---------- Register / Play ----------

        [Test]
        public void RegisterSound_NullOrEmptyId_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[SoundManager] SoundData Id is null or empty.");

            _manager.RegisterSound(new SoundData());
        }

        [Test]
        public void PlaySFX_Unregistered_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[SoundManager] Sound 'unknown' not registered.");

            _manager.PlaySFX("unknown");
        }

        [Test]
        public void PlayBGM_Unregistered_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[SoundManager] Sound 'unknown' not registered.");

            _manager.PlayBGM("unknown");
        }

        [Test]
        public void PlayVoice_Unregistered_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[SoundManager] Sound 'unknown' not registered.");

            _manager.PlayVoice("unknown");
        }

        [Test]
        public void StopSFX_Unregistered_LogsError()
        {
            LogAssert.Expect(LogType.Error, "[SoundManager] Sound 'unknown' not registered.");

            _manager.StopSFX("unknown");
        }
    }
}
