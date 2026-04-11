using System;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 배너 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewBannerData", menuName = "PublicFramework/Gacha/BannerData")]
    public class GachaBannerData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string _bannerId;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField] private Sprite _bannerImage;
        [SerializeField] private GachaType _bannerType;

        [Header("드롭 테이블")]
        [SerializeField] private DropTable _dropTable;

        [Header("비용")]
        [SerializeField] private int _pullCostSingle;
        [SerializeField] private int _pullCostMulti;
        [SerializeField] private string _costCurrencyId;

        [Header("기간")]
        [SerializeField] private string _startDate;
        [SerializeField] private string _endDate;

        [Header("천장 설정")]
        [SerializeField] private PityType _pityType;
        [SerializeField] private int _hardPityCount;
        [SerializeField] private int _softPityStartCount;
        [SerializeField] private float _softPityRateIncrease;
        [SerializeField] private PityCarryPolicy _pityCarryPolicy;

        [Header("Multi 보장")]
        [SerializeField] private int _multiPullCount = 10;
        [SerializeField] private ItemGrade _multiGuaranteedMinGrade;

        [Header("픽업")]
        [SerializeField] private string[] _pickupItemIds;

        [Header("중복 처리")]
        [SerializeField] private DuplicatePolicy _duplicatePolicy;

        public string BannerId => _bannerId;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite BannerImage => _bannerImage;
        public GachaType BannerType => _bannerType;
        public DropTable DropTable => _dropTable;
        public int PullCostSingle => _pullCostSingle;
        public int PullCostMulti => _pullCostMulti;
        public string CostCurrencyId => _costCurrencyId;
        public string StartDate => _startDate;
        public string EndDate => _endDate;
        public PityType PityType => _pityType;
        public int HardPityCount => _hardPityCount;
        public int SoftPityStartCount => _softPityStartCount;
        public float SoftPityRateIncrease => _softPityRateIncrease;
        public PityCarryPolicy PityCarryPolicy => _pityCarryPolicy;
        public int MultiPullCount => _multiPullCount;
        public ItemGrade MultiGuaranteedMinGrade => _multiGuaranteedMinGrade;
        public IReadOnlyList<string> PickupItemIds => _pickupItemIds;
        public DuplicatePolicy DuplicatePolicy => _duplicatePolicy;
    }
}
