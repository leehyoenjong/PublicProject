using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// SkillSystem 부팅 진입점.
    /// Awake 에서 BuffDataIndex 를 빌드하고, SkillSystem 을 조립/등록한 뒤 SkillDataCollection 의 스킬을 모두 등록한다.
    /// Update 에서 Tick 을 구동한다.
    /// </summary>
    public class SkillSystemInitializer : MonoBehaviour
    {
        [SerializeField] private BuffDataCollection _buffDataCollection;
        [SerializeField] private SkillDataCollection _skillDataCollection;

        private SkillSystem _skillSystem;

        public ISkillSystem SkillSystem => _skillSystem;

        private void Awake()
        {
            BuildBuffIndex();
            _skillSystem = CreateSkillSystem();
            ServiceLocator.Register<ISkillSystem>(_skillSystem);
            RegisterSkills();
            InjectMonsterAIRegistry();
        }

        // 몬스터 BT 의 액션 레지스트리를 MonsterSystem 에 주입한다(없으면 _treeExecutor=null 이라 TickAI 가 즉시 종료).
        // CastSkill 액션이 실제 ISkillSystem 으로 시전하도록 SkillSystem 생성 직후 이 시점에서 연결한다.
        // GameBootstrapper([DefaultExecutionOrder(-1000)])가 먼저 돌아 IMonsterSystem 은 이미 등록돼 있다.
        private void InjectMonsterAIRegistry()
        {
            if (!ServiceLocator.Has<IMonsterSystem>())
            {
                Debug.LogWarning("[스킬초기화] IMonsterSystem 미등록 — 몬스터 BT 액션 레지스트리 주입 생략(몬스터 AI 비활성)");
                return;
            }

            IMonsterSystem monsterSystem = ServiceLocator.Get<IMonsterSystem>();
            monsterSystem.SetActionRegistry(BehaviorActionRegistry.CreateDefault(_skillSystem));
            Debug.Log("[스킬초기화] 몬스터 BT 액션 레지스트리 주입 완료 (CastSkill→SkillSystem)");
        }

        private void Update()
        {
            _skillSystem?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ISkillSystem>();
            Debug.Log("[스킬초기화] 파괴됨. ISkillSystem 등록 해제");
        }

        private void BuildBuffIndex()
        {
            if (_buffDataCollection == null)
            {
                Debug.LogWarning("[스킬초기화] BuffDataCollection 미주입 — ApplyBuff 액션 조회 실패 가능");
                BuffDataIndex.Build(null);
                return;
            }

            BuffDataIndex.Build(_buffDataCollection.Items);
            int count = _buffDataCollection.Items != null ? _buffDataCollection.Items.Count : 0;
            Debug.Log($"[스킬초기화] BuffDataIndex 구축: {count}개 항목");
        }

        private SkillSystem CreateSkillSystem()
        {
            IEventBus eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            IBuffSystem buffSystem = ServiceLocator.Has<IBuffSystem>() ? ServiceLocator.Get<IBuffSystem>() : null;
            IStatSystem statSystem = ServiceLocator.Has<IStatSystem>() ? ServiceLocator.Get<IStatSystem>() : null;
            ISoundManager soundManager = ServiceLocator.Has<ISoundManager>() ? ServiceLocator.Get<ISoundManager>() : null;
            IObjectPoolManager objectPool = ServiceLocator.Has<IObjectPoolManager>() ? ServiceLocator.Get<IObjectPoolManager>() : null;

            if (eventBus == null)
                Debug.LogWarning("[스킬초기화] IEventBus 미등록 — 스킬 이벤트 미발행");
            if (buffSystem == null)
                Debug.LogWarning("[스킬초기화] IBuffSystem 미등록 — ApplyBuff 액션 무효");

            return new SkillSystem(eventBus, buffSystem, statSystem, soundManager, objectPool);
        }

        private void RegisterSkills()
        {
            if (_skillDataCollection == null || _skillDataCollection.Items == null || _skillDataCollection.Items.Count == 0)
            {
                Debug.LogWarning("[스킬초기화] SkillDataCollection 비어있음 — 등록된 스킬 없음");
                return;
            }

            int registered = 0;
            foreach (SkillData data in _skillDataCollection.Items)
            {
                if (data == null) continue;
                _skillSystem.RegisterSkill(data);
                registered++;
            }
            Debug.Log($"[스킬초기화] {registered}개 스킬 등록됨");
        }
    }
}
