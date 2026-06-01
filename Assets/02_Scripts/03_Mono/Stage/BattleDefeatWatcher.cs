using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// [샘플/기본 구현] 감시 진영(_watchFaction, 기본 Friendly) 전멸 시
    /// IStageSystem.ReportStageFail(AllDead) 를 호출해 전투 "패배" 루프를 종결한다.
    ///
    /// 배경: 프레임워크는 "승리(적 전멸 → Wave 전환/클리어)"만 StageBattleHost 가 처리한다.
    /// 패배 반쪽(아군 전멸)은 비어 있었으므로 이 컴포넌트가 채운다.
    /// 의도적으로 부팅(GameBootstrapper)에 자동 등록하지 않는다 — 전투 씬에 직접 부착하는 opt-in 방식.
    /// 파생 프로젝트는 부활/리트라이/페이즈 전환 등 규칙에 맞게 교체하거나 제거한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class BattleDefeatWatcher : MonoBehaviour
    {
        [Header("감시 진영 (전멸 시 패배 보고)")]
        [SerializeField] private Faction _watchFaction = Faction.Friendly;

        private IEventBus _eventBus;
        private IStageSystem _stageSystem;
        private System.Action<UnitDiedEvent> _onUnitDied;
        private bool _resolved;

        private void OnEnable()
        {
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            _stageSystem = ServiceLocator.Has<IStageSystem>() ? ServiceLocator.Get<IStageSystem>() : null;

            if (_eventBus == null)
            {
                Debug.LogWarning("[전투패배] IEventBus 미등록 — 감시 비활성", this);
                return;
            }

            _resolved = false;
            _onUnitDied = OnUnitDied;
            _eventBus.Subscribe(_onUnitDied);
            Debug.Log($"[전투패배] {_watchFaction} 전멸 감시 시작");
        }

        private void OnDisable()
        {
            if (_eventBus != null && _onUnitDied != null) _eventBus.Unsubscribe(_onUnitDied);
        }

        private void OnUnitDied(UnitDiedEvent ev)
        {
            if (_resolved || _stageSystem == null) return;
            // 사망 보고 시점엔 해당 UnitController._isAlive 가 아직 갱신 전일 수 있어 본인은 제외하고 센다.
            if (CountAlive(_watchFaction, ev.InstanceId) > 0) return;

            _resolved = true;
            Debug.Log($"[전투패배] {_watchFaction} 전멸 — 스테이지 실패 보고(AllDead)");
            _stageSystem.ReportStageFail(StageLoseCondition.AllDead);
        }

        private int CountAlive(Faction faction, string excludeInstanceId)
        {
            UnitController[] all = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                UnitController u = all[i];
                if (u == null || !u.IsAlive || u.Faction != faction) continue;
                if (u.InstanceId == excludeInstanceId) continue;
                count++;
            }
            return count;
        }
    }
}
