using System;
using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 엔티티 1체의 스탯 보유·계산기. 4단계 계산: (Base + Flat) × (1 + Percent) × Multiplicative.
    /// 기본 스탯 enum 15종 + 커스텀 dict(string 키) 하이브리드. 성장 커브, 스냅샷, 히스토리, 분해, 재생 틱 지원.
    /// CurrentHP/CurrentMP 는 상태값(스탯과 별도)으로 이 컨테이너에 보관.
    /// </summary>
    public interface IStatContainer
    {
        string OwnerId { get; }
        int Level { get; }

        // 현재 상태값 (스탯 아님)
        float CurrentHP { get; }
        float CurrentMP { get; }
        bool IsAlive { get; }

        // 기본 스탯 조회/설정
        float GetFinalValue(StatType type);
        float GetBaseValue(StatType type);
        void SetBaseValue(StatType type, float value);
        void SetGrowthCurve(StatType type, LevelCurve curve);
        void RegisterCustomCurve(string key, Func<int, float> formula);

        // 커스텀 스탯 (Dictionary 기반)
        float GetFinalValue(string customKey);
        float GetBaseValue(string customKey);
        void SetBaseValue(string customKey, float value);
        void SetGrowthCurve(string customKey, LevelCurve curve);
        IReadOnlyCollection<string> CustomKeys { get; }

        // Modifier 추가/제거
        void AddModifier(IStatModifier modifier);
        bool RemoveModifier(IStatModifier modifier);
        int RemoveModifiersFromSource(object source);
        IReadOnlyList<IStatModifier> GetModifiers(StatLayer layer);
        IReadOnlyList<IStatModifier> AllModifiers { get; }

        // 레벨 변경 (성장 커브 적용)
        void SetLevel(int level);

        // CurrentHP/MP 조작
        void SetCurrentHP(float hp);
        void SetCurrentMP(float mp);
        void ResetToMax();          // CurrentHP/MP 를 최대값으로
        void Kill();                // CurrentHP = 0
        void Revive();              // CurrentHP = MaxHP
        float MaxMP { get; }        // 커스텀 키 "MP" 의 final 또는 0

        // 분해 API
        StatDecomposition GetDecomposition(StatType type);
        StatDecomposition GetDecomposition(string customKey);

        // 스냅샷
        IStatSnapshot TakeSnapshot();
        void RestoreSnapshot(IStatSnapshot snapshot);

        // 히스토리
        IReadOnlyList<StatHistoryEntry> GetHistory(int? limit = null);
        void ClearHistory();
        int HistoryCapacity { get; set; }

        // 재생 틱 + 임시 modifier 만료
        void Tick(float deltaTime);

        // 전체 재계산 트리거 (드물게 사용)
        void RecalculateAll();
    }

    /// <summary>
    /// IStatContainer 의 동결된 상태 스냅샷. 전투 시작 시 캡처해 전투 중 변경 무효화 등에 사용.
    /// </summary>
    public interface IStatSnapshot
    {
        string OwnerId { get; }
        int Level { get; }
        DateTime CapturedAtUtc { get; }
    }
}
