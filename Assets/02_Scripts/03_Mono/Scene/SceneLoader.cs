using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PublicFramework
{
    public class SceneLoader : ISceneLoader
    {
        private const string DEFAULT_LOADING_SCENE = "LoadingScene";

        private ISceneFlow _flow;
        private MonoBehaviour _coroutineRunner;
        private ISceneParam _pendingParam;
        private string _targetScene;
        private string _loadingSceneName;

        public string CurrentScene { get; private set; }
        public bool IsLoading { get; private set; }

        public event Action<string> OnSceneChanged;

        public SceneLoader(MonoBehaviour coroutineRunner, string loadingSceneName = DEFAULT_LOADING_SCENE)
        {
            _coroutineRunner = coroutineRunner;
            _loadingSceneName = loadingSceneName;
            CurrentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[씬] 초기화 시작. 현재 씬: {CurrentScene}, 로딩 씬: {_loadingSceneName}");
        }

        public void SetLoadingScene(string sceneName)
        {
            _loadingSceneName = sceneName;
            Debug.Log($"[씬] 로딩 씬 설정됨: {_loadingSceneName}");
        }

        public void SetFlow(ISceneFlow flow)
        {
            _flow = flow;
            Debug.Log($"[씬] 플로우 설정됨: {flow.GetType().Name}");
        }

        public void LoadScene(string sceneName, ISceneParam param = null)
        {
            if (IsLoading)
            {
                Debug.LogWarning("[씬] 이미 로딩 중. 요청 무시됨.");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[씬] 씬 이름이 null이거나 비어 있음.");
                return;
            }

            _targetScene = sceneName;
            _pendingParam = param;
            IsLoading = true;

            Debug.Log($"[씬] 로드 요청: {CurrentScene} -> {sceneName}");
            _coroutineRunner.StartCoroutine(LoadSequence());
        }

        private IEnumerator LoadSequence()
        {
            string previousScene = CurrentScene;

            // Step 1: Exit current scene
            if (_flow != null)
            {
                bool exitDone = false;
                _flow.OnSceneExit(previousScene, () => exitDone = true);
                while (!exitDone) yield return null;
            }

            // Step 2: Load loading scene (Additive)
            AsyncOperation loadingOp = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);
            if (loadingOp != null)
            {
                while (!loadingOp.isDone) yield return null;
            }

            _flow?.OnLoadingStart();

            // Step 3: Unload previous scene
            if (!string.IsNullOrEmpty(previousScene) && previousScene != _loadingSceneName)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(previousScene);
                if (unloadOp != null)
                {
                    while (!unloadOp.isDone) yield return null;
                }
            }

            // Step 4: Async load target scene
            AsyncOperation targetOp = SceneManager.LoadSceneAsync(_targetScene, LoadSceneMode.Additive);
            if (targetOp != null)
            {
                targetOp.allowSceneActivation = false;

                while (targetOp.progress < 0.9f)
                {
                    _flow?.OnLoadProgress(targetOp.progress / 0.9f);
                    yield return null;
                }

                _flow?.OnLoadProgress(1f);
                targetOp.allowSceneActivation = true;

                while (!targetOp.isDone) yield return null;
            }

            // Step 5: Scene loaded + Init
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(_targetScene));
            _flow?.OnSceneLoaded(_targetScene);
            _flow?.OnSceneInit(_pendingParam);

            // Step 6: Fade out
            if (_flow != null)
            {
                bool fadeDone = false;
                _flow.OnFadeOut(() => fadeDone = true);
                while (!fadeDone) yield return null;
            }

            // Step 7: Unload loading scene + Ready
            AsyncOperation unloadLoadingOp = SceneManager.UnloadSceneAsync(_loadingSceneName);
            if (unloadLoadingOp != null)
            {
                while (!unloadLoadingOp.isDone) yield return null;
            }

            _flow?.OnSceneReady();

            CurrentScene = _targetScene;
            IsLoading = false;
            _pendingParam = null;

            Debug.Log($"[씬] 씬 변경됨: {previousScene} -> {CurrentScene}");
            OnSceneChanged?.Invoke(CurrentScene);
        }
    }
}
