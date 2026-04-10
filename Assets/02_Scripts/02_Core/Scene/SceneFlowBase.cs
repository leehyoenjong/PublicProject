using System;

namespace PublicFramework
{
    public abstract class SceneFlowBase : ISceneFlow
    {
        public virtual void OnSceneExit(string currentScene, Action onComplete) => onComplete?.Invoke();
        public virtual void OnLoadingStart() { }
        public virtual void OnLoadProgress(float progress) { }
        public virtual void OnSceneLoaded(string newScene) { }
        public virtual void OnSceneInit(ISceneParam param) { }
        public virtual void OnFadeOut(Action onComplete) => onComplete?.Invoke();
        public virtual void OnSceneReady() { }
    }
}
