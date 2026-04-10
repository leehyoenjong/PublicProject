using System;

namespace PublicFramework
{
    public interface ISceneLoader : IService
    {
        string CurrentScene { get; }
        bool IsLoading { get; }
        void LoadScene(string sceneName, ISceneParam param = null);
        void SetFlow(ISceneFlow flow);
        event Action<string> OnSceneChanged;
    }
}
