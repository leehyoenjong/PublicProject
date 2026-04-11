using System;

namespace PublicFramework
{
    /// <summary>
    /// 강화 비용 구조체
    /// </summary>
    public struct EnhanceCost
    {
        public EnhanceMaterialEntry[] Materials;
        public bool CanAfford;
    }

    [Serializable]
    public struct EnhanceMaterialEntry
    {
        public EnhanceMaterialType MaterialType;
        public int Amount;
    }
}
