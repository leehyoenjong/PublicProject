using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 펫 추종 위치 계산기. PetFollowStrategy 별 desiredPosition 을 순수 함수로 산출한다.
    /// MonoBehaviour 분리 — PetFollowAdapter 가 매 프레임 호출, EditMode 테스트로 직접 검증.
    /// 좌표계는 2D X·Y 평면 가정 (Z 는 항상 ownerPos.Z 유지).
    /// </summary>
    public static class PetFollowSolver
    {
        /// <summary>
        /// 추종 전략에 따른 목표 위치 계산.
        /// </summary>
        /// <param name="ownerFacing">오너의 마지막 이동 방향(2D, normalized). 0 벡터면 +X 로 폴백.</param>
        /// <param name="orbitAngleRad">Orbit 전략에서 사용하는 누적 각도(라디안). 다른 전략에선 무시.</param>
        public static Vector3 ComputeDesiredPosition(
            PetFollowStrategy strategy,
            Vector3 ownerPos,
            Vector2 ownerFacing,
            float followDistance,
            float orbitAngleRad)
        {
            Vector2 facing = ownerFacing.sqrMagnitude > 0.0001f ? ownerFacing.normalized : Vector2.right;

            switch (strategy)
            {
                case PetFollowStrategy.Behind:
                    return ownerPos - new Vector3(facing.x, facing.y, 0f) * followDistance;

                case PetFollowStrategy.Side:
                    Vector2 perp = new Vector2(-facing.y, facing.x);
                    return ownerPos + new Vector3(perp.x, perp.y, 0f) * followDistance;

                case PetFollowStrategy.Orbit:
                    return ownerPos + new Vector3(Mathf.Cos(orbitAngleRad), Mathf.Sin(orbitAngleRad), 0f) * followDistance;

                case PetFollowStrategy.Aerial:
                    return ownerPos + new Vector3(-facing.x * followDistance * 0.5f, followDistance, 0f);

                case PetFollowStrategy.Hover:
                    return ownerPos + new Vector3(0f, followDistance, 0f);

                default:
                    return ownerPos;
            }
        }

        /// <summary>
        /// 펫이 오너로부터 catchUpDistance 보다 멀어졌는지 판정. 0 이하면 항상 false.
        /// </summary>
        public static bool ShouldCatchUp(Vector3 current, Vector3 owner, float catchUpDistance)
        {
            if (catchUpDistance <= 0f) return false;
            return (current - owner).sqrMagnitude > catchUpDistance * catchUpDistance;
        }
    }
}
