using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 기본 타겟 전략: self 와 적대(FactionRules.IsHostile)이며 살아있는 후보 중 가장 가까운 유닛.
    /// 상태가 없는 순수 전략이라 단일 인스턴스(Instance)를 공유한다.
    /// </summary>
    public sealed class NearestHostileTargetSelector : ITargetSelector
    {
        public static readonly NearestHostileTargetSelector Instance = new NearestHostileTargetSelector();

        public UnitController Select(UnitController self, IReadOnlyList<UnitController> candidates)
        {
            if (self == null || candidates == null) return null;

            UnitController nearest = null;
            float nearestSqr = float.MaxValue;
            Vector3 origin = self.transform.position;

            for (int i = 0; i < candidates.Count; i++)
            {
                UnitController u = candidates[i];
                if (u == null || u == self || !u.IsAlive) continue;
                if (!FactionRules.IsHostile(self.Faction, u.Faction)) continue;

                float sqr = (u.transform.position - origin).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearestSqr = sqr;
                nearest = u;
            }

            return nearest;
        }
    }
}
