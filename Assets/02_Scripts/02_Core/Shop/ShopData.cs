using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 상점 상품 정의 SO. 시트 ShopData(본체) + ShopReward(자식 테이블) 가 주입.
    /// 상점 타입 구분은 속성으로 처리 — 별도 서브타입 SO 없음.
    /// </summary>
    [CreateAssetMenu(fileName = "NewShopData", menuName = "PublicFramework/Shop/ShopData")]
    public class ShopData : ScriptableObject, IShopProduct
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private string _shopId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("icon")] private Sprite _icon;

        [Header("지불")]
        [SerializeField] private PaymentType _paymentType;
        [SerializeField] private string _paymentId;
        [SerializeField] private int _paymentAmount;

        [Header("갱신 주기")]
        [SerializeField] private ResetPeriod _resetPeriod;
        [SerializeField] private DayOfWeekMask _weeklyMask;
        [SerializeField] private string _eventStartUtc;
        [SerializeField] private string _eventEndUtc;

        [Header("재고 / 제한")]
        [SerializeField] private int _productLimit;
        [SerializeField] private int _playerLimit;
        [SerializeField] private LimitScope _playerLimitScope;

        [Header("할인")]
        [SerializeField] private int _discountPercent;
        [SerializeField] private int _firstPurchaseBonusPercent;

        [Header("노출 조건")]
        [SerializeField] private ShopConditionType _conditionType;
        [SerializeField] private string _conditionValue;

        [Header("UI")]
        [SerializeField] private int _slotIndex;
        [SerializeField] private bool _isFeatured;
        [SerializeField] private bool _isActive;

        [Header("보상 (ChildTable 주입)")]
        [SerializeField] private ShopReward[] _rewards;

        public string MID => _shopId;
        public int DisplayNameKey => _displayName;
        public int DescriptionKey => _description;
        public Sprite Icon => _icon;
        public PaymentType PaymentType => _paymentType;
        public string PaymentId => _paymentId;
        public int PaymentAmount => _paymentAmount;
        public ResetPeriod ResetPeriod => _resetPeriod;
        public DayOfWeekMask WeeklyMask => _weeklyMask;
        public string EventStartUtc => _eventStartUtc;
        public string EventEndUtc => _eventEndUtc;
        public int ProductLimit => _productLimit;
        public int PlayerLimit => _playerLimit;
        public LimitScope PlayerLimitScope => _playerLimitScope;
        public int DiscountPercent => _discountPercent;
        public int FirstPurchaseBonusPercent => _firstPurchaseBonusPercent;
        public ShopConditionType ConditionType => _conditionType;
        public string ConditionValue => _conditionValue;
        public int SlotIndex => _slotIndex;
        public bool IsFeatured => _isFeatured;
        public bool IsActive => _isActive;
        public IReadOnlyList<ShopReward> Rewards => _rewards;
    }
}
