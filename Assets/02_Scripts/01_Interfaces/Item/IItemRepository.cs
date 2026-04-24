using System.Collections.Generic;

namespace PublicFramework
{
    public interface IItemRepository
    {
        IItem GetItem(int mid);
        bool TryGetItem(int mid, out IItem item);
        IReadOnlyList<IItem> GetAll();
        bool TryGetSubtype<T>(int subtypeMID, out T subtype) where T : class, IItemSubtypeInfo;
    }
}

