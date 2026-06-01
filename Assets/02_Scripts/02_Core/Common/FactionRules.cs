namespace PublicFramework
{
    /// <summary>
    /// 진영 간 적대 관계 판정의 단일 출처(SRP). 기본 규칙: Friendly 와 Enemy 만 서로 적대,
    /// Neutral 은 누구와도 적대하지 않는다.
    /// 더 복잡한 관계(파벌/우호도/PvP 팀)가 필요하면 파생 프로젝트가 이 규칙을 대체한다(빈칸 확장 지점).
    /// </summary>
    public static class FactionRules
    {
        /// <summary>a 가 b 를 적으로 간주하는가. 대칭(IsHostile(a,b) == IsHostile(b,a)).</summary>
        public static bool IsHostile(Faction a, Faction b)
        {
            if (a == Faction.Neutral || b == Faction.Neutral) return false;
            return a != b;
        }
    }
}
