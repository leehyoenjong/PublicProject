using UnityEngine;

namespace PublicFramework
{
    public class SceneTransitionRunner : MonoBehaviour
    {
        [SerializeField] private string _firstScene;
        [SerializeField] private string _loadingSceneName = "LoadingScene";

        private SceneLoader _sceneLoader;

        public ISceneLoader SceneLoader => _sceneLoader;

        private void Awake()
        {
            _sceneLoader = new SceneLoader(this, _loadingSceneName);
            ServiceLocator.Register<ISceneLoader>(_sceneLoader);
            Debug.Log("[SceneTransitionRunner] Init started.");
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_firstScene))
            {
                _sceneLoader.LoadScene(_firstScene);
                Debug.Log($"[SceneTransitionRunner] Loading first scene: {_firstScene}");
            }
        }

        public void SetFlowAndLoad(ISceneFlow flow, string sceneName, ISceneParam param = null)
        {
            _sceneLoader.SetFlow(flow);
            _sceneLoader.LoadScene(sceneName, param);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ISceneLoader>();
            Debug.Log("[SceneTransitionRunner] Destroyed.");
        }
    }
}
