using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 승리 모달. StageClearedEvent 데이터로 별점/경과시간/첫클리어를 표시한다.
    /// "확인" 은 기본적으로 로비(_nextScene)로 돌아간다.
    ///
    /// 의도적 빈칸(파생 확장): 보상 목록 표시(StageClearedEvent 가 보상을 싣지 않음),
    /// 별점 아이콘 연출, "다음 스테이지" 진입.
    /// </summary>
    public class VictoryPopup : BasePopup
    {
        [Header("표시")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private TMP_Text _timeText;

        [Header("버튼")]
        [SerializeField] private Button _confirmButton;
        [SerializeField, SceneName] private string _nextScene = "01_Lobby";

        public override void Show(object data)
        {
            base.Show(data);

            if (data is StageClearedEvent ev)
            {
                int stars = Mathf.Clamp(ev.Stars, 0, 3);
                if (_titleText != null) _titleText.text = ev.IsFirstClear ? "첫 클리어!" : "승리";
                if (_starsText != null) _starsText.text = new string('★', stars) + new string('☆', 3 - stars);
                if (_timeText != null) _timeText.text = $"{ev.ElapsedSeconds:F1}s";
            }
        }

        private void OnEnable()
        {
            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirm);
        }

        private void OnDisable()
        {
            if (_confirmButton != null) _confirmButton.onClick.RemoveListener(OnConfirm);
        }

        private void OnConfirm()
        {
            SetResult(PopupResult.Confirm);
            if (!string.IsNullOrEmpty(_nextScene)) SceneManager.LoadScene(_nextScene);
        }
    }
}
