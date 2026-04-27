using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// IStatSnapshot 기본 구현. StatContainer.TakeSnapshot 이 생성하고 RestoreSnapshot 이 소비.
    /// 컨테이너의 모든 변동 가능 상태(Base/Curve/Modifier 사본/CurrentHP/CurrentMP/Level)를 보관.
    /// </summary>
    public class StatSnapshot : IStatSnapshot
    {
        public string OwnerId { get; }
        public int Level { get; }
        public DateTime CapturedAtUtc { get; }

        internal Dictionary<StatType, float> BaseValues { get; }
        internal Dictionary<string, float> CustomBaseValues { get; }
        internal Dictionary<StatType, LevelCurve> Curves { get; }
        internal Dictionary<string, LevelCurve> CustomCurves { get; }
        internal List<IStatModifier> Modifiers { get; }
        internal float CurrentHP { get; }
        internal float CurrentMP { get; }

        internal StatSnapshot(
            string ownerId,
            int level,
            DateTime capturedAtUtc,
            Dictionary<StatType, float> baseValues,
            Dictionary<string, float> customBaseValues,
            Dictionary<StatType, LevelCurve> curves,
            Dictionary<string, LevelCurve> customCurves,
            List<IStatModifier> modifiers,
            float currentHP,
            float currentMP)
        {
            OwnerId = ownerId;
            Level = level;
            CapturedAtUtc = capturedAtUtc;
            BaseValues = baseValues;
            CustomBaseValues = customBaseValues;
            Curves = curves;
            CustomCurves = customCurves;
            Modifiers = modifiers;
            CurrentHP = currentHP;
            CurrentMP = currentMP;
        }
    }
}
