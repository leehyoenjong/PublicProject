using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 타겟 선택 전략(OCP seam). 후보 목록에서 self 가 공격할 대상 하나를 고른다.
    /// 기본 구현은 NearestHostileTargetSelector(가장 가까운 적대 유닛).
    /// 파생 프로젝트는 최약체/최고위협/도발(taunt)/후열 우선 등으로 교체한다 — 호출부 수정 없이 주입.
    ///
    /// 후보 수집(누가 씬에 있는가)은 호출자 책임이다. 타겟 "선택 규칙"만 이 인터페이스가 담당(SRP).
    /// </summary>
    public interface ITargetSelector
    {
        /// <summary>candidates 중 대상 1체 반환(없으면 null). self/null/사망/비적대는 구현이 걸러낸다.</summary>
        UnitController Select(UnitController self, IReadOnlyList<UnitController> candidates);
    }
}
