using UnityEngine;
using UnityEngine.SceneManagement;

namespace PublicFramework
{
    /// <summary>
    /// Core 시스템 통합 부팅 진입점. Awake 에서 EventBus → StatSystem → BuffSystem → MonsterSystem
    /// → (SoundManager 옵션) 를 만들고 ServiceLocator 에 등록한다. SkillSystemInitializer/PoolInitializer
    /// 보다 먼저 실행되도록 [DefaultExecutionOrder(-1000)].
    /// 부팅 완료 후 _nextScene 으로 자동 전환. INIT 자신은 DontDestroyOnLoad 로 살아남는다.
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

        [Header("Item (선택 — IInventorySystem 등록 + ItemDataCollection 로 Repository 채움)")]
        [SerializeField] private ItemDataCollection _itemDataCollection;

        [Header("Scene 전환 (부팅 완료 후 자동 LoadScene)")]
        [SerializeField] private bool _loadNextSceneOnBoot = true;
        [SerializeField, SceneName] private string _nextScene = "02_Battle";

        private EventBus _eventBus;
        private StatSystem _statSystem;
        private BuffSystem _buffSystem;
        private MonsterSystem _monsterSystem;
        private SoundManager _soundManager;
        private StageSystem _stageSystem;
        private StageSelection _stageSelection;
        private InventorySystem _inventorySystem;
        private ItemDataRepository _itemRepo;
        private IRewardHandler _rewardHandler;
        private bool _isOwner;

        private void Awake()
        {
            if (ServiceLocator.Has<IEventBus>())
            {
                Debug.LogWarning("[부팅] 중복 진입점 감지 — 이 인스턴스는 파기");
                Destroy(gameObject);
                return;
            }

            _isOwner = true;

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

            // 씬 간 스테이지 선택 운반자 (로비 허브 → 전투 씬). 가벼워서 항상 등록.
            _stageSelection = new StageSelection();
            ServiceLocator.Register<IStageSelection>(_stageSelection);

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

            if (_itemDataCollection != null)
            {
                _itemRepo = new ItemDataRepository(_itemDataCollection);
                ServiceLocator.Register<IItemRepository>(_itemRepo);
                _inventorySystem = new InventorySystem(_itemRepo, _eventBus);
                ServiceLocator.Register<IInventorySystem>(_inventorySystem);
            }

            // 보상 핸들러: 스테이지/퀘스트/업적 보상을 인벤토리에 적립. Stage·Inventory 둘 다 있을 때만 연결.
            if (_stageSystem != null && _inventorySystem != null)
            {
                _rewardHandler = new InventoryRewardHandler(_inventorySystem);
                _stageSystem.SetRewardHandler(_rewardHandler);
            }

            Debug.Log($"[부팅] 핵심 시스템 등록됨: 이벤트버스 / 스탯 / 버프 / 몬스터{(_soundManager != null ? " / 사운드" : "")}{(_stageSystem != null ? " / 스테이지" : "")}{(_inventorySystem != null ? " / 인벤토리" : "")}");
        }

        // DontDestroyOnLoad + LoadScene 은 Start 로 미룸. Awake 에서 호출하면 INIT 이 즉시 DontDestroyOnLoad 씬으로
        // 옮겨져 같은 batch 의 다른 Awake (PoolInitializer 등) 가 prefab reference fake-null 을 만나는 케이스 회피.
        private void Start()
        {
            if (!_isOwner) return;

            DontDestroyOnLoad(gameObject);

            if (_loadNextSceneOnBoot && !string.IsNullOrEmpty(_nextScene))
            {
                Scene current = SceneManager.GetActiveScene();
                if (current.name == _nextScene)
                {
                    Debug.Log($"[부팅] 다음 씬 '{_nextScene}' 이 이미 활성화 — LoadScene 생략");
                }
                else
                {
                    Debug.Log($"[부팅] 다음 씬 로드: '{_nextScene}'");
                    SceneFlowRouter.Load(_nextScene);
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _statSystem?.TickAll(dt);
            _buffSystem?.Tick(dt);
        }

        private void OnDestroy()
        {
            if (!_isOwner) return;

            if (_inventorySystem != null) ServiceLocator.Unregister<IInventorySystem>();
            if (_itemRepo != null) ServiceLocator.Unregister<IItemRepository>();
            if (_stageSystem != null) ServiceLocator.Unregister<IStageSystem>();
            if (_soundManager != null) ServiceLocator.Unregister<ISoundManager>();
            ServiceLocator.Unregister<IStageSelection>();
            ServiceLocator.Unregister<IMonsterSystem>();
            ServiceLocator.Unregister<IBuffSystem>();
            ServiceLocator.Unregister<IStatSystem>();
            ServiceLocator.Unregister<IEventBus>();
        }
    }
}
