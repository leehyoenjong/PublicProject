using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 설정 SO. 실제 뽑기 비용/제한/천장/확률 전부 포함.
    /// 시트 GachaData(본체) + GachaTier(등급 확률) + GachaDrop(아이템 풀) 3탭으로 주입.
    /// Banner 는 컨테이너(노출/기간)일 뿐이고 뽑기 주체는 GachaData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGachaData", menuName = "PublicFramework/Gacha/GachaData")]
    public class GachaData : ScriptableObject, IGacha
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _gachaId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("icon")] private Sprite _icon;

        [Header("지불 (ShopData 와 공유)")]
        [SerializeField] private PaymentType _paymentType;
        [SerializeField] private int _cost1Item;
        [SerializeField] private int _cost1Amount;
        [SerializeField] private int _cost10Item;
        [SerializeField] private int _cost10Amount;

        [Header("제한")]
        [SerializeField] private int _dailyLimit;
        [SerializeField] private int _periodLimit;
        [SerializeField] private int _lifetimeLimit;

        [Header("10연 보너스")]
        [SerializeField] private bool _bonus11th;
        [SerializeField] private GuaranteedTier _bonusGuaranteedTier;

        [Header("천장")]
        [SerializeField] private int _pitySoftCount;
        [SerializeField] private int _pityHardCount;
        [SerializeField] private int _pityPickupCount;

        [Header("서브타입 (Phase 2 확장)")]
        [SerializeField] private GachaSubtype _subtypeType;
        [SerializeField] private string _subtypeRef;

        [Header("운영")]
        [SerializeField] private bool _isActive;

        [Header("등급 확률표 (GachaTier 자식 주입)")]
        [SerializeField] private GachaTierEntry[] _tiers;

        [Header("등급 내 아이템 풀 (GachaDrop 자식 주입)")]
        [SerializeField] private GachaDropEntry[] _drops;

        public string MID => _gachaId;
        public int DisplayNameKey => _displayName;
        public int DescriptionKey => _description;
        public Sprite Icon => _icon;
        public PaymentType PaymentType => _paymentType;
        public int Cost1Item => _cost1Item;
        public int Cost1Amount => _cost1Amount;
        public int Cost10Item => _cost10Item;
        public int Cost10Amount => _cost10Amount;
        public int DailyLimit => _dailyLimit;
        public int PeriodLimit => _periodLimit;
        public int LifetimeLimit => _lifetimeLimit;
        public bool Bonus11th => _bonus11th;
        public GuaranteedTier BonusGuaranteedTier => _bonusGuaranteedTier;
        public int PitySoftCount => _pitySoftCount;
        public int PityHardCount => _pityHardCount;
        public int PityPickupCount => _pityPickupCount;
        public GachaSubtype SubtypeType => _subtypeType;
        public string SubtypeRef => _subtypeRef;
        public bool IsActive => _isActive;
        public IReadOnlyList<GachaTierEntry> Tiers => _tiers;
        public IReadOnlyList<GachaDropEntry> Drops => _drops;
    }
}
