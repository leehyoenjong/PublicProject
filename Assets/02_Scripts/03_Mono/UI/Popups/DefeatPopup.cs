using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 패배 모달. StageFailedEvent 의 사유(AllDead/Timeout)를 표시한다.
    /// "재도전" 은 현재 씬을 다시 로드, "나가기" 는 로비(_lobbyScene)로 돌아간다.
    /// 재도전 시 스테이지 재진입 흐름은 전투 씬의 진입 트리거(StageBattleHost 등)에 위임한다.
    /// </summary>
    public class DefeatPopup : BasePopup
    {
        [Header("표시")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _reasonText;

        [Header("버튼")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _exitButton;
        [SerializeField, SceneName] private string _lobbyScene = "01_Lobby";

        public override void Show(object data)
        {
            base.Show(data);

            if (_titleText != null) _titleText.text = "패배";
            if (data is StageFailedEvent ev && _reasonText != null)
                _reasonText.text = ev.Reason == StageLoseCondition.Timeout ? "시간 초과" : "전멸";
        }

        private void OnEnable()
        {
            if (_retryButton != null) _retryButton.onClick.AddListener(OnRetry);
            if (_exitButton != null) _exitButton.onClick.AddListener(OnExit);
        }

        private void OnDisable()
        {
            if (_retryButton != null) _retryButton.onClick.RemoveListener(OnRetry);
            if (_exitButton != null) _exitButton.onClick.RemoveListener(OnExit);
        }

        private void OnRetry()
        {
            SetResult(PopupResult.Confirm);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnExit()
        {
            SetResult(PopupResult.Cancel);
            if (!string.IsNullOrEmpty(_lobbyScene)) SceneManager.LoadScene(_lobbyScene);
        }
    }
}
