using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// EnhanceData(EnhanceType 1종 = 1 SO) 묶음. Enhance 시트가 본체로 주입.
    /// 5개 row(Level/Grade/Transcend/Awakening/Evolution) 등록.
    /// </summary>
    [CreateAssetMenu(fileName = "EnhanceDataCollection", menuName = "PublicFramework/Enhance/Data Collection")]
    public class EnhanceDataCollection : DataCollection<EnhanceData>
    {
        public EnhanceData Find(EnhanceType enhanceType)
        {
            var list = Items;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].EnhanceType == enhanceType) return list[i];
            }
            return null;
        }
    }
}
