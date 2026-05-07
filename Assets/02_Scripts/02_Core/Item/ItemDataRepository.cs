using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// ItemDataCollection 을 인덱싱한 production IItemRepository.
    /// GameBootstrapper 가 부팅 시 ItemDataCollection 을 주입해 생성한다.
    /// TryGetSubtype 은 Equipment 등 subtype 도메인이 production 부팅된 후 본격 구현 — 현재 false 반환.
    /// </summary>
    public class ItemDataRepository : IItemRepository
    {
        private readonly Dictionary<int, IItem> _items = new();
        private readonly List<IItem> _all = new();

        public ItemDataRepository(ItemDataCollection collection)
        {
            if (collection == null || collection.Items == null)
            {
                Debug.LogWarning("[아이템] Repository 초기화: 컬렉션 비어있음");
                return;
            }

            foreach (ItemData data in collection.Items)
            {
                if (data == null) continue;
                _items[data.MID] = data;
                _all.Add(data);
            }
            Debug.Log($"[아이템] Repository 초기화 완료: {_all.Count}건 로드");
        }

        public IItem GetItem(int mid)
            => _items.TryGetValue(mid, out IItem item) ? item : null;

        public bool TryGetItem(int mid, out IItem item)
            => _items.TryGetValue(mid, out item);

        public IReadOnlyList<IItem> GetAll() => _all;

        public bool TryGetSubtype<T>(int subtypeMID, out T subtype)
            where T : class, IItemSubtypeInfo
        {
            subtype = null;
            return false;
        }
    }
}
