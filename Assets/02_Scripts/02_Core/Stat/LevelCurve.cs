using System;

namespace PublicFramework
{
    /// <summary>
    /// 레벨에 따른 스탯 성장 공식. SetGrowthCurve 로 적용하면 SetLevel 시점의 Base 가 자동 갱신된다.
    /// Custom 모드는 IStatContainer.RegisterCustomCurve 로 등록한 공식을 사용.
    /// </summary>
    [Serializable]
    public struct LevelCurve
    {
        public GrowthCurve Curve;
        public float Base;
        public float Growth;
        public string CustomKey;  // Custom 모드일 때 사용 — RegisterCustomCurve 의 키와 일치

        public LevelCurve(GrowthCurve curve, float baseValue, float growth, string customKey = null)
        {
            Curve = curve;
            Base = baseValue;
            Growth = growth;
            CustomKey = customKey;
        }

        public float Evaluate(int level, Func<int, float> customFn = null)
        {
            int lv = level < 1 ? 1 : level;
            return Curve switch
            {
                GrowthCurve.Linear => Base + lv * Growth,
                GrowthCurve.Quadratic => Base + lv * lv * Growth,
                GrowthCurve.Exponential => Base * (float)Math.Pow(1.0 + Growth, lv),
                GrowthCurve.Custom => customFn != null ? customFn(lv) : Base,
                _ => Base
            };
        }
    }
}
