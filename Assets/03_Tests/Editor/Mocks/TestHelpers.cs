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
