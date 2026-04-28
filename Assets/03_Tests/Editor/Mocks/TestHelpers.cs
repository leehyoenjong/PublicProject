using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 프레임워크 SO 의 private serializefield 를 테스트에서 채워주는 리플렉션 유틸.
    /// 테스트 전용 — 소스 코드에 생성자를 추가하지 않기 위함.
    /// </summary>
    internal static class TestHelpers
    {
        private const BindingFlags FIELD_FLAGS = BindingFlags.NonPublic | BindingFlags.Instance;

        public static GachaTierEntry MakeTier(GachaTierRank tier, int weight)
        {
            var entry = new GachaTierEntry();
            SetField(entry, "_tier", tier);
            SetField(entry, "_weight", weight);
            return entry;
        }

        public static GachaDropEntry MakeDrop(GachaTierRank tier, int itemMID, int weight)
        {
            var entry = new GachaDropEntry();
            SetField(entry, "_tier", tier);
            SetField(entry, "_itemMID", itemMID);
            SetField(entry, "_weight", weight);
            return entry;
        }

        public static BannerGachaEntry MakeBannerGacha(string gachaMID)
        {
            var entry = new BannerGachaEntry();
            SetField(entry, "_gachaMID", gachaMID);
            return entry;
        }

        public static ConditionData MakeConditionData(string id, ConditionType type, string targetId, int requiredAmount, string description = "")
        {
            var data = new ConditionData();
            SetField(data, "_conditionId", id);
            SetField(data, "_conditionType", type);
            SetField(data, "_targetId", targetId);
            SetField(data, "_requiredAmount", requiredAmount);
            SetField(data, "_description", description);
            return data;
        }

        public static QuestReward MakeQuestReward(int rewardId, int amount)
        {
            return new QuestReward(rewardId, amount);
        }

        public static QuestData MakeQuestData(
            string questId,
            QuestType questType,
            ConditionGroupType groupType,
            ConditionData[] conditions,
            QuestReward[] rewards = null,
            string[] prerequisiteIds = null,
            int requiredLevel = 0)
        {
            var data = ScriptableObject.CreateInstance<QuestData>();
            SetField(data, "_questId", questId);
            SetField(data, "_questType", questType);
            SetField(data, "_conditionGroupType", groupType);
            SetField(data, "_conditions", conditions ?? System.Array.Empty<ConditionData>());
            SetField(data, "_rewards", rewards ?? System.Array.Empty<QuestReward>());
            SetField(data, "_prerequisiteQuestIds", prerequisiteIds ?? System.Array.Empty<string>());
            SetField(data, "_requiredLevel", requiredLevel);
            return data;
        }

        public static AchievementTierData MakeAchievementTier(int requiredAmount, QuestReward[] rewards = null)
        {
            var tier = new AchievementTierData();
            SetField(tier, "_requiredAmount", requiredAmount);
            SetField(tier, "_rewards", rewards ?? System.Array.Empty<QuestReward>());
            SetField(tier, "_passiveStats", System.Array.Empty<PassiveStat>());
            return tier;
        }

        public static AchievementData MakeAchievementData(
            string achievementId,
            ConditionType conditionType,
            string targetId,
            AchievementTierData[] tiers,
            AchievementCategory category = AchievementCategory.Combat,
            bool isHidden = false)
        {
            var data = ScriptableObject.CreateInstance<AchievementData>();
            SetField(data, "_achievementId", achievementId);
            SetField(data, "_conditionType", conditionType);
            SetField(data, "_conditionTargetId", targetId);
            SetField(data, "_tiers", tiers ?? System.Array.Empty<AchievementTierData>());
            SetField(data, "_category", category);
            SetField(data, "_isHidden", isHidden);
            return data;
        }

        public static ShopReward MakeShopReward(int rewardItemMID, int amount)
        {
            var reward = new ShopReward();
            SetField(reward, "_rewardItemMID", rewardItemMID);
            SetField(reward, "_rewardAmount", amount);
            return reward;
        }

        public static ShopData MakeShopData(
            string mid,
            PaymentType paymentType = PaymentType.Item,
            int paymentAmount = 100,
            string paymentId = "9001",
            int productLimit = 0,
            int playerLimit = 0,
            LimitScope playerLimitScope = LimitScope.Day,
            ResetPeriod resetPeriod = ResetPeriod.None,
            ShopConditionType conditionType = ShopConditionType.None,
            string conditionValue = "",
            bool isActive = true,
            ShopReward[] rewards = null,
            string eventStartUtc = "",
            string eventEndUtc = "")
        {
            var data = ScriptableObject.CreateInstance<ShopData>();
            SetField(data, "_shopId", mid);
            SetField(data, "_paymentType", paymentType);
            SetField(data, "_paymentAmount", paymentAmount);
            SetField(data, "_paymentId", paymentId);
            SetField(data, "_productLimit", productLimit);
            SetField(data, "_playerLimit", playerLimit);
            SetField(data, "_playerLimitScope", playerLimitScope);
            SetField(data, "_resetPeriod", resetPeriod);
            SetField(data, "_conditionType", conditionType);
            SetField(data, "_conditionValue", conditionValue);
            SetField(data, "_isActive", isActive);
            SetField(data, "_rewards", rewards ?? System.Array.Empty<ShopReward>());
            SetField(data, "_eventStartUtc", eventStartUtc);
            SetField(data, "_eventEndUtc", eventEndUtc);
            return data;
        }

        public static TutorialStepData MakeTutorialStep(
            TutorialStepType stepType = TutorialStepType.Dialog,
            int dialogText = 0,
            DialogPosition dialogPosition = DialogPosition.Center,
            string highlightTargetId = "",
            HighlightShape highlightShape = HighlightShape.None,
            ArrowDirection arrowDirection = ArrowDirection.None,
            StepWaitType waitType = StepWaitType.None,
            float waitDuration = 0f,
            string waitConditionId = "",
            bool canSkip = false)
        {
            var step = new TutorialStepData();
            SetField(step, "_stepType", stepType);
            SetField(step, "_dialogText", dialogText);
            SetField(step, "_dialogPosition", dialogPosition);
            SetField(step, "_highlightTargetId", highlightTargetId);
            SetField(step, "_highlightShape", highlightShape);
            SetField(step, "_arrowDirection", arrowDirection);
            SetField(step, "_waitType", waitType);
            SetField(step, "_waitDuration", waitDuration);
            SetField(step, "_waitConditionId", waitConditionId);
            SetField(step, "_canSkip", canSkip);
            return step;
        }

        public static TutorialData MakeTutorialData(
            string tutorialId,
            TutorialStepData[] steps = null,
            TriggerType triggerType = TriggerType.Manual,
            string triggerValue = "",
            int priority = 0,
            bool canSkip = true,
            string[] prerequisiteIds = null)
        {
            var data = ScriptableObject.CreateInstance<TutorialData>();
            SetField(data, "_tutorialId", tutorialId);
            SetField(data, "_steps", steps ?? System.Array.Empty<TutorialStepData>());
            SetField(data, "_triggerType", triggerType);
            SetField(data, "_triggerValue", triggerValue);
            SetField(data, "_priority", priority);
            SetField(data, "_canSkip", canSkip);
            SetField(data, "_prerequisiteTutorialIds", prerequisiteIds ?? System.Array.Empty<string>());
            return data;
        }

        public static MailConfig MakeMailConfig(
            int maxMailCount = 100,
            float nearFullThreshold = 0.9f,
            int defaultExpiryDays = 30,
            int expiredAutoDeleteDays = 3,
            float expiredCheckInterval = 60f)
        {
            var config = ScriptableObject.CreateInstance<MailConfig>();
            SetField(config, "_maxMailCount", maxMailCount);
            SetField(config, "_nearFullThreshold", nearFullThreshold);
            SetField(config, "_defaultExpiryDays", defaultExpiryDays);
            SetField(config, "_expiredAutoDeleteDays", expiredAutoDeleteDays);
            SetField(config, "_expiredCheckInterval", expiredCheckInterval);
            return config;
        }

        public static LocalizationTable MakeLocalizationTable(LanguageCode language, params LocalizationEntry[] entries)
        {
            var table = ScriptableObject.CreateInstance<LocalizationTable>();
            table.SetData(language, entries ?? System.Array.Empty<LocalizationEntry>());
            return table;
        }

        public static SkillData MakeSkillData(
            string skillId,
            float cooldown = 5f,
            SkillCostType costType = SkillCostType.None,
            float costAmount = 0f,
            SkillActionEntry[] actions = null,
            SkillLevelEntry[] levelTable = null)
        {
            var data = ScriptableObject.CreateInstance<SkillData>();
            SetField(data, "_skillId", skillId);
            SetField(data, "_cooldown", cooldown);
            SetField(data, "_costType", costType);
            SetField(data, "_costAmount", costAmount);
            SetField(data, "_actions", actions ?? System.Array.Empty<SkillActionEntry>());
            SetField(data, "_levelTable", levelTable ?? System.Array.Empty<SkillLevelEntry>());
            return data;
        }

        public static BuffData MakeBuffData(
            string buffId,
            BuffCategory category = BuffCategory.Positive,
            DurationType durationType = DurationType.Timed,
            float duration = 10f,
            StackPolicy stackPolicy = StackPolicy.None,
            int maxStack = 1,
            RefreshPolicy refreshPolicy = RefreshPolicy.Reset,
            bool isUndispellable = false,
            float tickInterval = 0f,
            float tickValue = 0f,
            PassiveStat[] targetStats = null)
        {
            var data = ScriptableObject.CreateInstance<BuffData>();
            SetField(data, "_buffId", buffId);
            SetField(data, "_category", category);
            SetField(data, "_durationType", durationType);
            SetField(data, "_duration", duration);
            SetField(data, "_stackPolicy", stackPolicy);
            SetField(data, "_maxStack", maxStack);
            SetField(data, "_refreshPolicy", refreshPolicy);
            SetField(data, "_isUndispellable", isUndispellable);
            SetField(data, "_tickInterval", tickInterval);
            SetField(data, "_tickValue", tickValue);
            SetField(data, "_targetStats", targetStats ?? System.Array.Empty<PassiveStat>());
            return data;
        }

        public static ItemData MakeItemData(
            int mid,
            StackType stackType = StackType.Stack,
            int maxStack = 99,
            ItemCategory category = ItemCategory.Consumable,
            int convertRewardMID = 0,
            int convertRewardCount = 0)
        {
            var data = ScriptableObject.CreateInstance<ItemData>();
            SetField(data, "_itemId", mid);
            SetField(data, "_stackType", stackType);
            SetField(data, "_maxStack", maxStack);
            SetField(data, "_category", category);
            SetField(data, "_convertRewardMID", convertRewardMID);
            SetField(data, "_convertRewardCount", convertRewardCount);
            return data;
        }

        public static void InjectInventoryInstance(InventorySystem inventory, ItemInstance instance)
        {
            FieldInfo field = typeof(InventorySystem).GetField("_instances", FIELD_FLAGS);
            var dict = (Dictionary<string, ItemInstance>)field.GetValue(inventory);
            dict[instance.InstanceId] = instance;
        }

        public static CharacterDialogueEntry MakeCharacterDialogue(DialogueEvent ev, int lineKey)
        {
            var entry = new CharacterDialogueEntry();
            SetField(entry, "_event", ev);
            SetField(entry, "_lineKey", lineKey);
            return entry;
        }

        public static CharacterProfileEntry MakeCharacterProfile(string key, string value, int valueKey = 0)
        {
            var entry = new CharacterProfileEntry();
            SetField(entry, "_key", key);
            SetField(entry, "_value", value);
            SetField(entry, "_valueKey", valueKey);
            return entry;
        }

        public static CharacterInfo MakeCharacterInfo(
            int itemMID,
            CharacterRole role = CharacterRole.Dealer,
            string classTag = "",
            string elementTag = "",
            string baseStatMID = "",
            SkillData[] baseSkills = null,
            SkillSlotStrategy slotStrategy = SkillSlotStrategy.Fixed,
            int slotValue = 3,
            int defaultSkinMID = 0,
            string voiceSetId = "",
            string defaultPositionId = "",
            CharacterDialogueEntry[] dialogues = null,
            CharacterProfileEntry[] profiles = null)
        {
            var data = ScriptableObject.CreateInstance<CharacterInfo>();
            SetField(data, "_itemMID", itemMID);
            SetField(data, "_role", role);
            SetField(data, "_classTag", classTag);
            SetField(data, "_elementTag", elementTag);
            SetField(data, "_baseStatGroup", MakeStatGroupRef(baseStatMID));
            SetField(data, "_baseSkills", baseSkills ?? System.Array.Empty<SkillData>());
            SetField(data, "_slotStrategy", slotStrategy);
            SetField(data, "_slotValue", slotValue);
            SetField(data, "_defaultSkinMID", defaultSkinMID);
            SetField(data, "_voiceSetId", voiceSetId);
            SetField(data, "_defaultPositionId", defaultPositionId);
            SetField(data, "_dialogues", dialogues ?? System.Array.Empty<CharacterDialogueEntry>());
            SetField(data, "_profiles", profiles ?? System.Array.Empty<CharacterProfileEntry>());
            return data;
        }

        public static DeckConfig MakeDeckConfig(int maxDecks = 1, int partySize = 1, bool requireLeader = false, bool allowDuplicate = false)
        {
            var config = ScriptableObject.CreateInstance<DeckConfig>();
            SetField(config, "_maxDecks", maxDecks);
            SetField(config, "_partySize", partySize);
            SetField(config, "_requireLeader", requireLeader);
            SetField(config, "_allowDuplicate", allowDuplicate);
            return config;
        }

        public static MonsterInfo MakeMonsterInfo(
            string mid,
            MonsterType type = MonsterType.Normal,
            int nameKey = 0,
            int descKey = 0,
            string iconAddress = "",
            string classTag = "",
            string elementTag = "",
            string baseStatMID = "",
            SkillData[] baseSkills = null,
            string dropTableMID = "",
            string aiPresetMID = "",
            int level = 1,
            int expReward = 0,
            int goldReward = 0,
            string[] onSpawnEvents = null,
            string[] onDeathEvents = null,
            string hitReactionId = "")
        {
            var info = ScriptableObject.CreateInstance<MonsterInfo>();
            SetField(info, "_mid", mid);
            SetField(info, "_nameKey", nameKey);
            SetField(info, "_descKey", descKey);
            SetField(info, "_iconAddress", iconAddress);
            SetField(info, "_type", type);
            SetField(info, "_classTag", classTag);
            SetField(info, "_elementTag", elementTag);
            SetField(info, "_baseStatGroup", MakeStatGroupRef(baseStatMID));
            SetField(info, "_baseSkills", baseSkills ?? System.Array.Empty<SkillData>());
            SetField(info, "_dropTableMID", dropTableMID);
            SetField(info, "_aiPresetMID", aiPresetMID);
            SetField(info, "_level", level);
            SetField(info, "_expReward", expReward);
            SetField(info, "_goldReward", goldReward);
            SetField(info, "_onSpawnEvents", onSpawnEvents ?? System.Array.Empty<string>());
            SetField(info, "_onDeathEvents", onDeathEvents ?? System.Array.Empty<string>());
            SetField(info, "_hitReactionId", hitReactionId);
            return info;
        }

        public static DropEntry MakeDropEntry(
            int itemMID,
            int weight,
            int order = 1,
            int minCount = 1,
            int maxCount = 1,
            int minPlayerLevel = 0,
            int repeatLimit = 0)
        {
            var entry = new DropEntry();
            SetField(entry, "_order", order);
            SetField(entry, "_itemMID", itemMID);
            SetField(entry, "_weight", weight);
            SetField(entry, "_minCount", minCount);
            SetField(entry, "_maxCount", maxCount);
            SetField(entry, "_minPlayerLevel", minPlayerLevel);
            SetField(entry, "_repeatLimit", repeatLimit);
            return entry;
        }

        public static DropTableData MakeDropTableData(string mid, params DropEntry[] entries)
        {
            var data = ScriptableObject.CreateInstance<DropTableData>();
            SetField(data, "_mid", mid);
            SetField(data, "_entries", entries ?? System.Array.Empty<DropEntry>());
            return data;
        }

        public static MonsterEventCatalogEntry MakeMonsterEventCatalogEntry(
            string eventId, MonsterEventKind kind = MonsterEventKind.Death, int descKey = 0)
        {
            var entry = new MonsterEventCatalogEntry();
            SetField(entry, "_eventId", eventId);
            SetField(entry, "_kind", kind);
            SetField(entry, "_descKey", descKey);
            return entry;
        }

        public static MonsterEventCatalog MakeMonsterEventCatalog(params MonsterEventCatalogEntry[] entries)
        {
            var catalog = ScriptableObject.CreateInstance<MonsterEventCatalog>();
            SetField(catalog, "_entries", entries ?? System.Array.Empty<MonsterEventCatalogEntry>());
            return catalog;
        }

        public static PetInfo MakePetInfo(
            string mid,
            int itemMID = 0,
            int nameKey = 0,
            int descKey = 0,
            string iconAddress = "",
            PetRole roles = PetRole.None,
            string classTag = "",
            string elementTag = "",
            string baseStatMID = "",
            SkillData[] baseSkills = null,
            int skillSlotMax = 0,
            string aiPresetMID = "",
            PetFollowStrategy followStrategy = PetFollowStrategy.Behind,
            float followDistance = 0f,
            float catchUpDistance = 0f,
            PetCollisionPolicy collisionPolicy = PetCollisionPolicy.Ghost,
            string[] onAcquireEvents = null,
            string[] onEquipEvents = null,
            string[] onUnequipEvents = null)
        {
            var info = ScriptableObject.CreateInstance<PetInfo>();
            SetField(info, "_mid", mid);
            SetField(info, "_itemMID", itemMID);
            SetField(info, "_nameKey", nameKey);
            SetField(info, "_descKey", descKey);
            SetField(info, "_iconAddress", iconAddress);
            SetField(info, "_roles", roles);
            SetField(info, "_classTag", classTag);
            SetField(info, "_elementTag", elementTag);
            SetField(info, "_baseStatGroup", MakeStatGroupRef(baseStatMID));
            SetField(info, "_baseSkills", baseSkills ?? System.Array.Empty<SkillData>());
            SetField(info, "_skillSlotMax", skillSlotMax);
            SetField(info, "_aiPresetMID", aiPresetMID);
            SetField(info, "_followStrategy", followStrategy);
            SetField(info, "_followDistance", followDistance);
            SetField(info, "_catchUpDistance", catchUpDistance);
            SetField(info, "_collisionPolicy", collisionPolicy);
            SetField(info, "_onAcquireEvents", onAcquireEvents ?? System.Array.Empty<string>());
            SetField(info, "_onEquipEvents", onEquipEvents ?? System.Array.Empty<string>());
            SetField(info, "_onUnequipEvents", onUnequipEvents ?? System.Array.Empty<string>());
            return info;
        }

        // ===== Stage =====

        public static StarConditionEntry MakeStarCondition(ConditionType type, int amount)
        {
            var entry = new StarConditionEntry();
            SetField(entry, "_conditionType", type);
            SetField(entry, "_amount", amount);
            return entry;
        }

        public static WaveMonsterEntry MakeWaveMonster(string monsterMID, int count = 1, float spawnTiming = 0f, SpawnPattern pattern = SpawnPattern.Simultaneous)
        {
            var entry = new WaveMonsterEntry();
            SetField(entry, "_monsterMID", monsterMID);
            SetField(entry, "_count", count);
            SetField(entry, "_spawnTiming", spawnTiming);
            SetField(entry, "_spawnPattern", pattern);
            return entry;
        }

        public static BossPhaseEntry MakeBossPhase(float hpThreshold, string patternHook = "")
        {
            var entry = new BossPhaseEntry();
            SetField(entry, "_hpThreshold", hpThreshold);
            SetField(entry, "_patternHook", patternHook);
            return entry;
        }

        public static WaveData MakeWaveData(
            WaveMonsterEntry[] monsters = null,
            WaveTransitionCondition transition = WaveTransitionCondition.AllKill,
            string transitionTargetMonsterMID = "",
            float transitionTimer = 0f,
            BossPhaseEntry[] bossPhases = null,
            string bossEntryHook = "",
            string phaseTransitionHook = "")
        {
            var data = new WaveData();
            SetField(data, "_monsters", monsters ?? System.Array.Empty<WaveMonsterEntry>());
            SetField(data, "_transitionCondition", transition);
            SetField(data, "_transitionTargetMonsterMID", transitionTargetMonsterMID);
            SetField(data, "_transitionTimer", transitionTimer);
            SetField(data, "_bossPhases", bossPhases ?? System.Array.Empty<BossPhaseEntry>());
            SetField(data, "_bossEntryHook", bossEntryHook);
            SetField(data, "_phaseTransitionHook", phaseTransitionHook);
            return data;
        }

        public static StageEventEntry MakeStageEvent(
            StageEventType eventType,
            string targetId,
            StageEventTrigger trigger,
            string triggerValue = "",
            QuestReward[] rewards = null,
            int description = 0,
            bool canRepeat = false)
        {
            var entry = new StageEventEntry();
            SetField(entry, "_eventType", eventType);
            SetField(entry, "_targetId", targetId);
            SetField(entry, "_triggerType", trigger);
            SetField(entry, "_triggerValue", triggerValue);
            SetField(entry, "_rewardItems", rewards ?? System.Array.Empty<QuestReward>());
            SetField(entry, "_description", description);
            SetField(entry, "_canRepeat", canRepeat);
            return entry;
        }

        public static ChapterData MakeChapterData(
            string chapterId,
            ChapterType chapterType = ChapterType.Normal,
            int order = 0,
            string unlockChapterId = "",
            QuestReward[] completeRewards = null)
        {
            var data = ScriptableObject.CreateInstance<ChapterData>();
            SetField(data, "_chapterId", chapterId);
            SetField(data, "_chapterType", chapterType);
            SetField(data, "_sortOrder", order);
            SetField(data, "_unlockChapterId", unlockChapterId);
            SetField(data, "_chapterCompleteRewards", completeRewards ?? System.Array.Empty<QuestReward>());
            return data;
        }

        public static StageData MakeStageData(
            string stageId,
            string chapterId = "",
            StageType stageType = StageType.Normal,
            int order = 0,
            string[] prereqStageIds = null,
            int requiredLevel = 0,
            string[] requiredItemIds = null,
            int dailyEnterLimit = 0,
            int staminaCost = 0,
            string ticketItemId = "",
            int ticketAmount = 0,
            string currencyId = "",
            int currencyAmount = 0,
            StarConditionEntry[] starConditions = null,
            bool autoUnlocked = false,
            bool sweepEnabled = false,
            float timeLimitSeconds = 0f,
            StageWinCondition winCondition = StageWinCondition.AllKill,
            StageLoseCondition loseCondition = StageLoseCondition.AllDead,
            QuestReward[] firstClearRewards = null,
            QuestReward[] repeatRewards = null,
            QuestReward[] sweepRewards = null,
            WaveData[] waves = null,
            StageEventEntry[] events = null)
        {
            var data = ScriptableObject.CreateInstance<StageData>();
            SetField(data, "_stageId", stageId);
            SetField(data, "_chapterId", chapterId);
            SetField(data, "_stageType", stageType);
            SetField(data, "_sortOrder", order);
            SetField(data, "_prerequisiteStageIds", prereqStageIds ?? System.Array.Empty<string>());
            SetField(data, "_requiredLevel", requiredLevel);
            SetField(data, "_requiredItemIds", requiredItemIds ?? System.Array.Empty<string>());
            SetField(data, "_dailyEnterLimit", dailyEnterLimit);
            SetField(data, "_staminaCost", staminaCost);
            SetField(data, "_ticketItemId", ticketItemId);
            SetField(data, "_ticketAmount", ticketAmount);
            SetField(data, "_currencyId", currencyId);
            SetField(data, "_currencyAmount", currencyAmount);
            SetField(data, "_starConditions", starConditions ?? System.Array.Empty<StarConditionEntry>());
            SetField(data, "_autoUnlocked", autoUnlocked);
            SetField(data, "_sweepEnabled", sweepEnabled);
            SetField(data, "_timeLimitSeconds", timeLimitSeconds);
            SetField(data, "_winCondition", winCondition);
            SetField(data, "_loseCondition", loseCondition);
            SetField(data, "_firstClearRewards", firstClearRewards ?? System.Array.Empty<QuestReward>());
            SetField(data, "_repeatRewards", repeatRewards ?? System.Array.Empty<QuestReward>());
            SetField(data, "_sweepRewards", sweepRewards ?? System.Array.Empty<QuestReward>());
            SetField(data, "_waves", waves ?? System.Array.Empty<WaveData>());
            SetField(data, "_events", events ?? System.Array.Empty<StageEventEntry>());
            return data;
        }

        public static StageConfig MakeStageConfig(int maxStamina = 120, float recoverSeconds = 360f, bool autoBattleAllowed = false)
        {
            var config = ScriptableObject.CreateInstance<StageConfig>();
            SetField(config, "_maxStamina", maxStamina);
            SetField(config, "_staminaRecoverSeconds", recoverSeconds);
            SetField(config, "_autoBattleAllowedByDefault", autoBattleAllowed);
            return config;
        }

        // ===== Behavior Tree =====

        public static BehaviorNodeEntry MakeBehaviorNode(
            BehaviorNodeType nodeType,
            int[] childIndices = null,
            string actionKey = "",
            string param1 = "",
            string param2 = "",
            string param3 = "")
        {
            var entry = new BehaviorNodeEntry();
            SetField(entry, "_nodeType", nodeType);
            SetField(entry, "_childIndices", childIndices ?? System.Array.Empty<int>());
            SetField(entry, "_actionKey", actionKey);
            SetField(entry, "_param1", param1);
            SetField(entry, "_param2", param2);
            SetField(entry, "_param3", param3);
            return entry;
        }

        public static BehaviorTreePreset MakeBehaviorTreePreset(string presetId, int rootIndex, BehaviorNodeEntry[] nodes)
        {
            var preset = ScriptableObject.CreateInstance<BehaviorTreePreset>();
            SetField(preset, "_presetId", presetId);
            SetField(preset, "_rootIndex", rootIndex);
            SetField(preset, "_nodes", nodes ?? System.Array.Empty<BehaviorNodeEntry>());
            return preset;
        }

        // ===== Stat =====

        public static StatGroupData MakeStatGroupData(string statGroupId, params StatDataEntry[] entries)
        {
            var sg = ScriptableObject.CreateInstance<StatGroupData>();
            SetField(sg, "_statGroupId", statGroupId);
            SetField(sg, "_entries", entries ?? System.Array.Empty<StatDataEntry>());
            return sg;
        }

        private static StatGroupData MakeStatGroupRef(string statGroupId)
        {
            if (string.IsNullOrEmpty(statGroupId)) return null;
            var sg = ScriptableObject.CreateInstance<StatGroupData>();
            SetField(sg, "_statGroupId", statGroupId);
            return sg;
        }

        public static void SetPrivateField(object target, string fieldName, object value)
        {
            SetField(target, fieldName, value);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = FindField(target.GetType(), fieldName);
            if (field == null) throw new System.ArgumentException($"Field '{fieldName}' not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private static FieldInfo FindField(System.Type type, string fieldName)
        {
            System.Type t = type;
            while (t != null)
            {
                FieldInfo f = t.GetField(fieldName, FIELD_FLAGS);
                if (f != null) return f;
                t = t.BaseType;
            }
            return null;
        }
    }
}
