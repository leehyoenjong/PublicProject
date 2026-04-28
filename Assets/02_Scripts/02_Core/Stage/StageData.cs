using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 정의 ScriptableObject. StageData 시트 1행 = 1 SO.
    /// waves 는 자식 시트, events 는 자식 시트로 각각 주입.
    /// </summary>
    [CreateAssetMenu(fileName = "NewStageData", menuName = "PublicFramework/Stage/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _stageId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("chapterMID")] private string _chapterId;
        [SerializeField, SheetAlias("order")] private int _sortOrder;
        [SerializeField] private StageType _stageType;

        [Header("입장 조건")]
        [SerializeField, SheetAlias("prereqStageMIDs")] private string[] _prerequisiteStageIds;
        [SerializeField, SheetAlias("reqLevel")] private int _requiredLevel;
        [SerializeField, SheetAlias("reqItemMIDs")] private string[] _requiredItemIds;
        [SerializeField] private int _dailyEnterLimit;

        [Header("입장 비용")]
        [SerializeField] private int _staminaCost;
        [SerializeField, SheetAlias("ticketItemMID")] private string _ticketItemId;
        [SerializeField] private int _ticketAmount;
        [SerializeField, SheetAlias("currencyMID")] private string _currencyId;
        [SerializeField] private int _currencyAmount;

        [Header("별점 / 자동전투 / 소탕")]
        [SerializeField] private StarConditionEntry[] _starConditions;
        [SerializeField] private bool _autoUnlocked;
        [SerializeField] private bool _sweepEnabled;

        [Header("맵 정보")]
        [SerializeField] private string _sceneKey;
        [SerializeField] private string _bgmId;
        [SerializeField] private string _backgroundId;
        [SerializeField] private string _envEffectId;

        [Header("승패")]
        [SerializeField] private float _timeLimitSeconds;
        [SerializeField] private StageWinCondition _winCondition;
        [SerializeField] private StageLoseCondition _loseCondition;

        [Header("보상")]
        [SerializeField] private QuestReward[] _firstClearRewards;
        [SerializeField] private QuestReward[] _repeatRewards;
        [SerializeField] private QuestReward[] _sweepRewards;

        [Header("웨이브 (자식 시트)")]
        [SerializeField] private WaveData[] _waves;

        [Header("이벤트 (자식 시트)")]
        [SerializeField] private StageEventEntry[] _events;

        public string StageId => _stageId;
        public int DisplayName => _displayName;
        public int Description => _description;
        public string ChapterId => _chapterId;
        public int SortOrder => _sortOrder;
        public StageType StageType => _stageType;

        public IReadOnlyList<string> PrerequisiteStageIds => _prerequisiteStageIds;
        public int RequiredLevel => _requiredLevel;
        public IReadOnlyList<string> RequiredItemIds => _requiredItemIds;
        public int DailyEnterLimit => _dailyEnterLimit;

        public int StaminaCost => _staminaCost;
        public string TicketItemId => _ticketItemId;
        public int TicketAmount => _ticketAmount;
        public string CurrencyId => _currencyId;
        public int CurrencyAmount => _currencyAmount;

        public IReadOnlyList<StarConditionEntry> StarConditions => _starConditions;
        public bool AutoUnlocked => _autoUnlocked;
        public bool SweepEnabled => _sweepEnabled;

        public string SceneKey => _sceneKey;
        public string BgmId => _bgmId;
        public string BackgroundId => _backgroundId;
        public string EnvEffectId => _envEffectId;

        public float TimeLimitSeconds => _timeLimitSeconds;
        public StageWinCondition WinCondition => _winCondition;
        public StageLoseCondition LoseCondition => _loseCondition;

        public IReadOnlyList<QuestReward> FirstClearRewards => _firstClearRewards;
        public IReadOnlyList<QuestReward> RepeatRewards => _repeatRewards;
        public IReadOnlyList<QuestReward> SweepRewards => _sweepRewards;

        public IReadOnlyList<WaveData> Waves => _waves;
        public IReadOnlyList<StageEventEntry> Events => _events;
    }
}
