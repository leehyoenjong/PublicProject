using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 종료(승리/패배) 이벤트를 받아 결과 모달을 띄우는 발표자.
    /// UI가 사는 전투 씬에 부착한다. 코어(GameBootstrapper) + UIBootstrapper(IPopupManager) 위에서 동작.
    /// 표시할 팝업 ID 는 UIBootstrapper 에 등록한 ID 와 맞춘다(기본 victory/defeat).
    /// </summary>
    [DisallowMultipleComponent]
    public class StageResultPresenter : MonoBehaviour
    {
        [Header("팝업 ID (UIBootstrapper 등록 ID 와 일치)")]
        [SerializeField] private string _victoryPopupId = "victory";
        [SerializeField] private string _defeatPopupId = "defeat";

        private IEventBus _eventBus;
        private System.Action<StageClearedEvent> _onCleared;
        private System.Action<StageFailedEvent> _onFailed;

        private void OnEnable()
        {
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            if (_eventBus == null)
            {
                Debug.LogWarning("[결과발표] IEventBus 미등록 — 비활성", this);
                return;
            }

            _onCleared = OnCleared;
            _onFailed = OnFailed;
            _eventBus.Subscribe(_onCleared);
            _eventBus.Subscribe(_onFailed);
        }

        private void OnDisable()
        {
            if (_eventBus == null) return;
            if (_onCleared != null) _eventBus.Unsubscribe(_onCleared);
            if (_onFailed != null) _eventBus.Unsubscribe(_onFailed);
        }

        private void OnCleared(StageClearedEvent ev) => ShowPopup(_victoryPopupId, ev);
        private void OnFailed(StageFailedEvent ev) => ShowPopup(_defeatPopupId, ev);

        private void ShowPopup(string popupId, object data)
        {
            if (string.IsNullOrEmpty(popupId)) return;
            if (!ServiceLocator.Has<IPopupManager>())
            {
                Debug.LogWarning($"[결과발표] IPopupManager 미등록 — '{popupId}' 표시 생략", this);
                return;
            }

            ServiceLocator.Get<IPopupManager>().Show(popupId, data);
            Debug.Log($"[결과발표] 결과 모달 표시: {popupId}");
        }
    }
}
