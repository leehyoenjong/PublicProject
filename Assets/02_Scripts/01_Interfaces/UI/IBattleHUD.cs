using UnityEngine;

namespace PublicFramework
{
    public interface IBattleHUD
    {
        void SetupHUD(BattleHUDConfig config);
        void UpdateHP(string unitId, float ratio);
        void UpdateSkill(string skillId, float coolRatio);
        void ShowDamageText(Vector3 position, int value, DamageType type);
        void UpdateWave(int current, int total);
        void Cleanup();
    }
}
