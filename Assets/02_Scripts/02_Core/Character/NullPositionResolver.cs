using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 포지션 기능을 쓰지 않는 프로젝트용 기본 Resolver. 모든 positionId 를 (0,0) 으로 해석하고 항상 유효하다고 답한다.
    /// </summary>
    public class NullPositionResolver : IPositionResolver
    {
        public bool TryResolve(string positionId, out Vector2 worldOffset)
        {
            worldOffset = Vector2.zero;
            return true;
        }

        public bool IsValid(string positionId) => true;
    }
}
