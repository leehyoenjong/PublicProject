using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 드롭 아이템의 fly-to-UI 경로 계산. start→target 직선 lerp + sine 곡선 arc 옵션.
    /// 순수 함수 — DropFlyCollector 가 매 프레임 호출, EditMode 테스트로 직접 검증.
    /// </summary>
    public static class FlyTrajectory
    {
        /// <summary>
        /// start → target 사이 정규화 t(0~1) 위치. arcHeight > 0 이면 t=0.5 에서 최고점 +arcHeight 만큼 위로 솟음.
        /// 양 끝(t=0, t=1)은 sin(0)=sin(π)=0 이라 arc 영향 없음.
        /// </summary>
        public static Vector3 Evaluate(Vector3 start, Vector3 target, float t, float arcHeight = 0f)
        {
            float clamped = Mathf.Clamp01(t);
            Vector3 linear = Vector3.Lerp(start, target, clamped);
            if (arcHeight <= 0f) return linear;

            float arc = Mathf.Sin(clamped * Mathf.PI) * arcHeight;
            return new Vector3(linear.x, linear.y + arc, linear.z);
        }
    }
}
