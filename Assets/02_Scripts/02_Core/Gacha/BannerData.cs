using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 배너 컨테이너 SO. 실제 뽑기 설정은 GachaData 에 있다.
    /// 시트 BannerData(본체) + BannerGacha(자식 gachaMID 목록) 로 주입.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBannerData", menuName = "PublicFramework/Gacha/BannerData")]
    public class BannerData : ScriptableObject, IBanner
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _bannerId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("keyVisual")] private Sprite _keyVisual;

        [Header("분류")]
        [SerializeField] private BannerCategory _category;

        [Header("기간")]
        [SerializeField] private string _periodStartUtc;
        [SerializeField] private string _periodEndUtc;

        [Header("UI")]
        [SerializeField] private int _displayOrder;

        [Header("해금 조건")]
        [SerializeField] private BannerUnlockType _unlockType;
        [SerializeField] private string _unlockValue;

        [Header("운영")]
        [SerializeField] private bool _carryOverCounter;
        [SerializeField] private bool _isActive;

        [Header("포함 가챠 (BannerGacha 자식 주입)")]
        [SerializeField] private BannerGachaEntry[] _gachas;

        public string MID => _bannerId;
        public int DisplayNameKey => _displayName;
        public int DescriptionKey => _description;
        public Sprite KeyVisual => _keyVisual;
        public BannerCategory Category => _category;
        public string PeriodStartUtc => _periodStartUtc;
        public string PeriodEndUtc => _periodEndUtc;
        public int DisplayOrder => _displayOrder;
        public BannerUnlockType UnlockType => _unlockType;
        public string UnlockValue => _unlockValue;
        public bool CarryOverCounter => _carryOverCounter;
        public bool IsActive => _isActive;
        public IReadOnlyList<BannerGachaEntry> Gachas => _gachas;
    }
}
