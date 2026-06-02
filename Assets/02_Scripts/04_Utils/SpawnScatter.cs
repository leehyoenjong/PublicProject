using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스폰 위치 분산 유틸(anti-stack). 황금각 나선으로 인덱스를 균일하게 퍼뜨려
    /// 같은 지점에 여러 개체가 겹쳐 스폰되는 것을 막는다. 결정론적(같은 index→같은 오프셋)이라 테스트 가능.
    /// </summary>
    public static class SpawnScatter
    {
        // 황금각(라디안). 나선 배치 시 인접 인덱스가 항상 떨어져 균일 분산된다.
        private const float GOLDEN_ANGLE = 2.39996323f;
        // 점밀도 보정 — 반경 성장 속도를 spacing 대비 적당히 낮춰 과도하게 멀어지지 않게.
        private const float DENSITY = 0.6f;

        /// <summary>
        /// index 번째 개체의 분산 오프셋(z=0 평면). spacing 은 대략적인 개체 간 간격 스케일.
        /// index 0 은 (거의) 원점, 이후 나선으로 균일하게 바깥으로 퍼진다.
        /// </summary>
        public static Vector3 SpiralOffset(int index, float spacing)
        {
            if (index < 0) index = 0;
            if (spacing <= 0f) spacing = 1f;

            float angle = index * GOLDEN_ANGLE;
            float radius = spacing * Mathf.Sqrt(index + 0.5f) * DENSITY;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }
    }
}
