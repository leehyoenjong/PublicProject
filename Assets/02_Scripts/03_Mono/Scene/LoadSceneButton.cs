using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 버튼 클릭 시 지정한 씬으로 전환한다. 로비→배틀 등 화면 전환 버튼에 범용 사용.
    /// 부팅 시스템(GameBootstrapper 등)은 DontDestroyOnLoad 로 유지되므로 단일 LoadScene 으로 충분하다.
    /// PopupManager 와 동일하게 Button.onClick(UnityEvent) 어댑터 패턴을 따른다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LoadSceneButton : MonoBehaviour
    {
        [SerializeField, SceneName] private string _sceneName;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(Load);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Load);
        }

        private void Load()
        {
            if (string.IsNullOrEmpty(_sceneName))
            {
                Debug.LogError("[씬전환] 씬 이름 미설정 — 전환 취소");
                return;
            }

            Debug.Log($"[씬전환] 버튼 클릭 → 씬 로드: {_sceneName}");
            SceneFlowRouter.Load(_sceneName);
        }
    }
}
