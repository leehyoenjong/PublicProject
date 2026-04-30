using UnityEngine;

namespace PublicFramework
{
    public class BootScene : MonoBehaviour
    {
        [SerializeField] private string _startScene = "MainMenu";

        private void Start()
        {
            Debug.Log("[씬] 부트 시작.");

            ISceneLoader loader = ServiceLocator.Get<ISceneLoader>();
            if (loader == null)
            {
                Debug.LogError("[씬] ISceneLoader 미등록. SceneTransitionRunner 초기화 확인 필요.");
                return;
            }

            loader.LoadScene(_startScene);
            Debug.Log($"[씬] 시작 씬 로드: {_startScene}");
        }
    }
}
