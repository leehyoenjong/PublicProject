using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// positionId(string) 를 실제 좌표/효과로 변환. 프로젝트별 해석 전략.
    /// 포지션 기능을 쓰지 않으면 <see cref="NullPositionResolver"/> 를 그대로 사용.
    /// </summary>
    public interface IPositionResolver : IService
    {
        bool TryResolve(string positionId, out Vector2 worldOffset);
        bool IsValid(string positionId);
    }
}
