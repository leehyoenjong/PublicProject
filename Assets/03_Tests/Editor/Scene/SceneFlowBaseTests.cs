using System;
using NUnit.Framework;

namespace PublicFramework.Tests.Scene
{
    /// <summary>
    /// EditMode 부분 커버 — SceneLoader 본체는 SceneManager.LoadSceneAsync 의존이라 PlayMode 로 위임.
    /// 본 테스트는 SceneFlowBase 의 기본 onComplete 콜백 흐름과 override 훅 호출 순서만 검증.
    /// </summary>
    public class SceneFlowBaseTests
    {
        // ---------- 기본 동작 (default base) ----------

        [Test]
        public void OnSceneExit_Default_InvokesOnComplete()
        {
            var flow = new DefaultFlow();
            bool done = false;

            flow.OnSceneExit("Main", () => done = true);

            Assert.IsTrue(done);
        }

        [Test]
        public void OnSceneExit_NullCallback_DoesNotThrow()
        {
            var flow = new DefaultFlow();

            Assert.DoesNotThrow(() => flow.OnSceneExit("Main", null));
        }

        [Test]
        public void OnFadeOut_Default_InvokesOnComplete()
        {
            var flow = new DefaultFlow();
            bool done = false;

            flow.OnFadeOut(() => done = true);

            Assert.IsTrue(done);
        }

        [Test]
        public void OnFadeOut_NullCallback_DoesNotThrow()
        {
            var flow = new DefaultFlow();

            Assert.DoesNotThrow(() => flow.OnFadeOut(null));
        }

        [Test]
        public void Default_NoOpHooks_DoNotThrow()
        {
            var flow = new DefaultFlow();

            Assert.DoesNotThrow(() =>
            {
                flow.OnLoadingStart();
                flow.OnLoadProgress(0.5f);
                flow.OnSceneLoaded("InGame");
                flow.OnSceneInit(null);
                flow.OnSceneReady();
            });
        }

        // ---------- override 훅 ----------

        [Test]
        public void OverriddenFlow_AllHooks_AreInvokedAndCallbacksFire()
        {
            var flow = new RecordingFlow();
            bool exitCb = false;
            bool fadeCb = false;
            var param = new TestSceneParam();

            flow.OnSceneExit("Lobby", () => exitCb = true);
            flow.OnLoadingStart();
            flow.OnLoadProgress(0.42f);
            flow.OnSceneLoaded("InGame");
            flow.OnSceneInit(param);
            flow.OnFadeOut(() => fadeCb = true);
            flow.OnSceneReady();

            Assert.AreEqual("Lobby", flow.ExitedScene);
            Assert.IsTrue(exitCb);
            Assert.IsTrue(flow.LoadingStarted);
            Assert.AreEqual(0.42f, flow.LastProgress, 0.0001f);
            Assert.AreEqual("InGame", flow.LoadedScene);
            Assert.AreSame(param, flow.InitParam);
            Assert.IsTrue(flow.FadedOut);
            Assert.IsTrue(fadeCb);
            Assert.IsTrue(flow.Ready);
        }

        // ---------- Helpers ----------

        private class DefaultFlow : SceneFlowBase { }

        private class RecordingFlow : SceneFlowBase
        {
            public string ExitedScene;
            public bool LoadingStarted;
            public float LastProgress = -1f;
            public string LoadedScene;
            public ISceneParam InitParam;
            public bool FadedOut;
            public bool Ready;

            public override void OnSceneExit(string currentScene, Action onComplete)
            {
                ExitedScene = currentScene;
                base.OnSceneExit(currentScene, onComplete);
            }

            public override void OnLoadingStart() => LoadingStarted = true;
            public override void OnLoadProgress(float progress) => LastProgress = progress;
            public override void OnSceneLoaded(string newScene) => LoadedScene = newScene;
            public override void OnSceneInit(ISceneParam param) => InitParam = param;

            public override void OnFadeOut(Action onComplete)
            {
                FadedOut = true;
                base.OnFadeOut(onComplete);
            }

            public override void OnSceneReady() => Ready = true;
        }

        private class TestSceneParam : ISceneParam { }
    }
}
