using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IItemRepository. Register 로 IItem 을 주입.</summary>
    public class FakeItemRepository : IItemRepository
    {
        private readonly Dictionary<int, IItem> _items = new Dictionary<int, IItem>();

        public void Register(IItem item)
        {
            _items[item.MID] = item;
        }

        public IItem GetItem(int mid)
        {
            return _items.TryGetValue(mid, out IItem item) ? item : null;
        }

        public bool TryGetItem(int mid, out IItem item)
        {
            return _items.TryGetValue(mid, out item);
        }

        public IReadOnlyList<IItem> GetAll()
        {
            return new List<IItem>(_items.Values);
        }

        public bool TryGetSubtype<T>(int subtypeMID, out T subtype) where T : class, IItemSubtypeInfo
        {
            subtype = null;
            return false;
        }
    }
}
