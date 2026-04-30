using UnityEngine;

namespace PublicFramework
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "PublicFramework/Item/ItemData")]
    public class ItemData : ScriptableObject, IItem
    {
        [Header("기본 정보")]
        [SerializeField, SheetAlias("MID")] private int _itemId;
        [SerializeField, LocalizationKey, SheetAlias("name")] private int _displayName;
        [SerializeField, LocalizationKey, SheetAlias("desc")] private int _description;
        [SerializeField, SheetAlias("icon")] private Sprite _icon;

        [Header("분류")]
        [SerializeField] private Rarity _rarity;
        [SerializeField] private ItemCategory _category;

        [Header("스택")]
        [SerializeField] private StackType _stackType;
        [SerializeField] private int _maxStack = 1;

        [Header("Convert 치환 보상")]
        [SerializeField] private int _convertRewardMID;
        [SerializeField] private int _convertRewardCount;

        [Header("서브타입 SO (시트는 int MID, 임포터가 SO로 변환)")]
        [SerializeField, SheetAlias("subtypeMID"), DependsOnEntry(typeof(EquipmentInfo))] private ScriptableObject _subtypeRef;

        public int MID => _itemId;
        public int DisplayNameKey => _displayName;
        public int DescriptionKey => _description;
        public Sprite Icon => _icon;
        public Rarity Rarity => _rarity;
        public ItemCategory Category => _category;
        public StackType StackType => _stackType;
        public int MaxStack => _maxStack <= 0 ? 1 : _maxStack;
        public int ConvertRewardMID => _convertRewardMID;
        public int ConvertRewardCount => _convertRewardCount;
        public IItemSubtypeInfo SubtypeRef => _subtypeRef as IItemSubtypeInfo;

        private void OnValidate()
        {
            if (_subtypeRef != null && _subtypeRef is not IItemSubtypeInfo)
            {
                Debug.LogWarning($"[아이템] MID={_itemId} subtypeRef는 IItemSubtypeInfo를 구현해야 함: {_subtypeRef.name}");
            }
        }
    }
}

