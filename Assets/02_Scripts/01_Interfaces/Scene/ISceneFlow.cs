using System;

namespace PublicFramework
{
    public interface ISceneFlow
    {
        void OnSceneExit(string currentScene, Action onComplete);
        void OnLoadingStart();
        void OnLoadProgress(float progress);
        void OnSceneLoaded(string newScene);
        void OnSceneInit(ISceneParam param);
        void OnFadeOut(Action onComplete);
        void OnSceneReady();
    }
}
