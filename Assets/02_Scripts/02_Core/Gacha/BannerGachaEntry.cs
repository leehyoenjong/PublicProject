using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BannerData 자식 테이블 — Banner 에 포함되는 GachaData MID 매핑.
    /// 시트 BannerGacha 의 parentId/order 는 예약어라 필드에 매핑하지 않는다.
    /// </summary>
    [System.Serializable]
    public class BannerGachaEntry
    {
        [SerializeField, SheetAlias("gachaMID")] private string _gachaMID;

        public string GachaMID => _gachaMID;
    }
}
