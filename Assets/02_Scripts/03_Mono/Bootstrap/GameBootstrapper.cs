using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// Core 시스템 통합 부팅 진입점. Awake 에서 EventBus → StatSystem → BuffSystem → MonsterSystem
    /// → (SoundManager 옵션) 를 만들고 ServiceLocator 에 등록한다. SkillSystemInitializer/PoolInitializer
    /// 보다 먼저 실행되도록 [DefaultExecutionOrder(-1000)].
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrapper : MonoBehaviour
    {
        [Header("Sound (선택 — ISoundManager 등록 여부)")]
        [SerializeField] private bool _enableSound = true;
        [SerializeField] private int _sfxPoolSize = 8;

        [Header("Monster (선택 — MonsterSystem.Initialize 입력)")]
        [SerializeField] private MonsterInfoCollection _monsterInfo;
        [SerializeField] private DropTableDataCollection _dropTables;
        [SerializeField] private MonsterEventCatalog _monsterEventCatalog;

        [Header("Stage (선택 — IStageSystem 등록 + chapter/stage 컬렉션 자동 등록)")]
        [SerializeField] private bool _enableStage = true;
        [SerializeField] private StageConfig _stageConfig;
        [SerializeField] private ChapterDataCollection _chapterCollection;
        [SerializeField] private StageDataCollection _stageCollection;
        [SerializeField] private int _initialUnlockLevel = 1;

        private EventBus _eventBus;
        private StatSystem _statSystem;
        private BuffSystem _buffSystem;
        private MonsterSystem _monsterSystem;
        private SoundManager _soundManager;
        private StageSystem _stageSystem;

        private void Awake()
        {
            _eventBus = new EventBus();
            ServiceLocator.Register<IEventBus>(_eventBus);

            _statSystem = new StatSystem(_eventBus);
            ServiceLocator.Register<IStatSystem>(_statSystem);

            _buffSystem = new BuffSystem(_statSystem, _eventBus);
            ServiceLocator.Register<IBuffSystem>(_buffSystem);

            _monsterSystem = new MonsterSystem(eventBus: _eventBus);
            ServiceLocator.Register<IMonsterSystem>(_monsterSystem);
            if (_monsterInfo != null)
                _monsterSystem.Initialize(_monsterInfo, _dropTables, _monsterEventCatalog);

            if (_enableSound)
            {
                _soundManager = new SoundManager(this, transform, _sfxPoolSize);
                ServiceLocator.Register<ISoundManager>(_soundManager);
            }

            if (_enableStage)
            {
                StageConfig cfg = _stageConfig != null ? _stageConfig : ScriptableObject.CreateInstance<StageConfig>();
                _stageSystem = new StageSystem(_eventBus, cfg);
                ServiceLocator.Register<IStageSystem>(_stageSystem);

                if (_chapterCollection != null && _chapterCollection.Items != null)
                {
                    foreach (ChapterData c in _chapterCollection.Items)
                        _stageSystem.RegisterChapter(c);
                }
                if (_stageCollection != null && _stageCollection.Items != null)
                {
                    foreach (StageData s in _stageCollection.Items)
                        _stageSystem.RegisterStage(s);
                }
                _stageSystem.CheckUnlocks(_initialUnlockLevel);
            }

            Debug.Log($"[부팅] 핵심 시스템 등록됨: 이벤트버스 / 스탯 / 버프 / 몬스터{(_soundManager != null ? " / 사운드" : "")}{(_stageSystem != null ? " / 스테이지" : "")}");
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _statSystem?.TickAll(dt);
            _buffSystem?.Tick(dt);
        }

        private void OnDestroy()
        {
            if (_stageSystem != null) ServiceLocator.Unregister<IStageSystem>();
            if (_soundManager != null) ServiceLocator.Unregister<ISoundManager>();
            ServiceLocator.Unregister<IMonsterSystem>();
            ServiceLocator.Unregister<IBuffSystem>();
            ServiceLocator.Unregister<IStatSystem>();
            ServiceLocator.Unregister<IEventBus>();
        }
    }
}
