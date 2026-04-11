using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 기본 확률 모델. 단순 Random + 천장 100% 보장.
    /// </summary>
    public class DefaultProbabilityModel : IProbabilityModel
    {
        public bool Roll(float baseProb, int pityCount, int maxPity)
        {
            if (maxPity > 0 && pityCount >= maxPity - 1)
            {
                Debug.Log("[ProbabilityModel] Pity reached — guaranteed success");
                return true;
            }

            float roll = Random.value;
            bool success = roll <= baseProb;

            Debug.Log($"[ProbabilityModel] Roll: {roll:F3} vs {baseProb:F3} → {(success ? "SUCCESS" : "FAIL")}");
            return success;
        }

        public float GetDisplayProb(float baseProb, int pityCount, int maxPity)
        {
            if (maxPity > 0 && pityCount >= maxPity - 1)
            {
                return 1f;
            }

            return baseProb;
        }
    }
}
