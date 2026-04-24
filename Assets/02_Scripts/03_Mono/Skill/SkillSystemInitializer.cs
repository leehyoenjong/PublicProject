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
        }

        private void Update()
        {
            _skillSystem?.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<ISkillSystem>();
            Debug.Log("[SkillSystemInitializer] Destroyed. ISkillSystem unregistered.");
        }

        private void BuildBuffIndex()
        {
            if (_buffDataCollection == null)
            {
                Debug.LogWarning("[SkillSystemInitializer] BuffDataCollection 미주입 — ApplyBuff 액션 조회 실패 가능");
                BuffDataIndex.Build(null);
                return;
            }

            BuffDataIndex.Build(_buffDataCollection.Items);
            int count = _buffDataCollection.Items != null ? _buffDataCollection.Items.Count : 0;
            Debug.Log($"[SkillSystemInitializer] BuffDataIndex built: {count} entries");
        }

        private SkillSystem CreateSkillSystem()
        {
            IEventBus eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            IBuffSystem buffSystem = ServiceLocator.Has<IBuffSystem>() ? ServiceLocator.Get<IBuffSystem>() : null;
            IStatSystem statSystem = ServiceLocator.Has<IStatSystem>() ? ServiceLocator.Get<IStatSystem>() : null;
            ISoundManager soundManager = ServiceLocator.Has<ISoundManager>() ? ServiceLocator.Get<ISoundManager>() : null;
            IObjectPoolManager objectPool = ServiceLocator.Has<IObjectPoolManager>() ? ServiceLocator.Get<IObjectPoolManager>() : null;

            if (eventBus == null)
                Debug.LogWarning("[SkillSystemInitializer] IEventBus 미등록 — 스킬 이벤트 미발행");
            if (buffSystem == null)
                Debug.LogWarning("[SkillSystemInitializer] IBuffSystem 미등록 — ApplyBuff 액션 무효");

            return new SkillSystem(eventBus, buffSystem, statSystem, soundManager, objectPool);
        }

        private void RegisterSkills()
        {
            if (_skillDataCollection == null || _skillDataCollection.Items == null || _skillDataCollection.Items.Count == 0)
            {
                Debug.LogWarning("[SkillSystemInitializer] SkillDataCollection 비어있음 — 등록된 스킬 없음");
                return;
            }

            int registered = 0;
            foreach (SkillData data in _skillDataCollection.Items)
            {
                if (data == null) continue;
                _skillSystem.RegisterSkill(data);
                registered++;
            }
            Debug.Log($"[SkillSystemInitializer] Registered {registered} skill(s).");
        }
    }
}
