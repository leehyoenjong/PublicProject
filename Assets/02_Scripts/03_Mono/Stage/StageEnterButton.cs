using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 선택 허브용 버튼. 클릭 시 IStageSelection 에 _stageId 를 기록하고 전투 씬을 로드한다.
    /// 전투 씬의 StageBattleHost 가 그 선택을 읽어 해당 스테이지로 진입한다(미등록이면 호스트 기본값).
    /// 로비에 스테이지 수만큼 배치하는 재사용 컴포넌트 — 레이아웃/연출은 파생 몫.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StageEnterButton : MonoBehaviour
    {
        [SerializeField] private string _stageId;
        [SerializeField, SceneName] private string _battleScene = "02_Battle";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (_button != null) _button.onClick.AddListener(Enter);
        }

        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(Enter);
        }

        private void Enter()
        {
            if (string.IsNullOrEmpty(_stageId))
            {
                Debug.LogError("[스테이지선택] _stageId 미설정 — 진입 취소");
                return;
            }

            if (ServiceLocator.Has<IStageSelection>())
            {
                ServiceLocator.Get<IStageSelection>().Select(_stageId);
            }
            else
            {
                Debug.LogWarning("[스테이지선택] IStageSelection 미등록 — 호스트 기본 스테이지로 진입");
            }

            if (string.IsNullOrEmpty(_battleScene))
            {
                Debug.LogError("[스테이지선택] 전투 씬 미설정 — 진입 취소");
                return;
            }

            Debug.Log($"[스테이지선택] 진입: {_stageId} → {_battleScene}");
            SceneManager.LoadScene(_battleScene);
        }
    }
}
