namespace PublicFramework
{
    /// <summary>
    /// 데미지 경감 공식(순수 C#). 기본식은 감쇠형 `dmg × (1 − DEF / (DEF + K))`.
    /// K(DefenseConstant) 가 클수록 같은 DEF 의 경감 효율이 낮아진다 — 파생 프로젝트가 밸런스에 맞게 튜닝한다.
    /// 결과는 절대 음수가 되지 않으며, DEF ≤ 0 이면 원본 데미지를 그대로 반환한다.
    ///
    /// 의도적 빈칸(파생 확장 지점): 크리티컬 · 회피 · 속성저항(MagicResist/ElementResist) · 고정관통.
    /// 이들은 프레임워크 기본 공식에 포함하지 않는다 — 게임마다 규칙이 다르기 때문.
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>방어 상수 기본값. K 가 클수록 DEF 효율 ↓. 파생은 Mitigate 의 defenseConstant 인자로 튜닝.</summary>
        public const float DEFAULT_DEFENSE_CONSTANT = 100f;

        /// <summary>
        /// rawDamage 에 방어 경감을 적용한 최종 데미지. rawDamage ≤ 0 이면 0,
        /// defense ≤ 0 이면 rawDamage 그대로.
        /// </summary>
        public static float Mitigate(float rawDamage, float defense, float defenseConstant = DEFAULT_DEFENSE_CONSTANT)
        {
            if (rawDamage <= 0f) return 0f;
            if (defense <= 0f) return rawDamage;

            float denom = defense + defenseConstant;
            if (denom <= 0f) return rawDamage;

            float multiplier = defenseConstant / denom; // == 1 - DEF/(DEF+K), 범위 (0, 1]
            float result = rawDamage * multiplier;
            return result < 0f ? 0f : result;
        }
    }
}
