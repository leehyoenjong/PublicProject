using UnityEngine;

namespace PublicFramework
{
    public class BootScene : MonoBehaviour
    {
        [SerializeField] private string _startScene = "MainMenu";

        private void Start()
        {
            Debug.Log("[BootScene] Boot started.");

            ISceneLoader loader = ServiceLocator.Get<ISceneLoader>();
            if (loader == null)
            {
                Debug.LogError("[BootScene] ISceneLoader not found. Ensure SceneTransitionRunner is initialized.");
                return;
            }

            loader.LoadScene(_startScene);
            Debug.Log($"[BootScene] Loading start scene: {_startScene}");
        }
    }
}
