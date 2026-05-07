using System;
using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>
    /// 테스트용 IInventorySystem. 재화/수량만 Dict 로 관리.
    /// 가챠 테스트에 필요한 최소 기능(GetCount/ConsumeByMID/AddItem) 만 구현.
    /// 치환(Convert) 시뮬레이션: SetConversion(mid, converted) 호출 시 AddItem 에서 Convert 결과 반환.
    /// </summary>
    public class FakeInventorySystem : IInventorySystem
    {
        private readonly Dictionary<int, int> _balances = new Dictionary<int, int>();
        private readonly Dictionary<int, ConvertedReward[]> _conversions = new Dictionary<int, ConvertedReward[]>();
        private readonly List<(int mid, int count, object source)> _addCalls = new List<(int, int, object)>();

        public IReadOnlyList<(int mid, int count, object source)> AddCalls => _addCalls;

        public void SetBalance(int mid, int count)
        {
            _balances[mid] = count;
        }

        /// <summary>특정 mid 를 AddItem 했을 때 치환이 일어나도록 설정.</summary>
        public void SetConversion(int originalMID, params ConvertedReward[] converted)
        {
            _conversions[originalMID] = converted;
        }

        public int GetCount(int mid)
        {
            return _balances.TryGetValue(mid, out int c) ? c : 0;
        }

        public int GetMaxStack(int mid) => int.MaxValue;

        public bool ConsumeByMID(int mid, int count)
        {
            int current = GetCount(mid);
            if (current < count) return false;
            _balances[mid] = current - count;
            return true;
        }

        public ItemAddResult AddItem(int mid, int count, object source)
        {
            _addCalls.Add((mid, count, source));

            if (_conversions.TryGetValue(mid, out ConvertedReward[] converted))
            {
                foreach (ConvertedReward r in converted)
                {
                    _balances[r.MID] = GetCount(r.MID) + r.Count;
                }
                return new ItemAddResult(true, mid, count, 0, null, converted);
            }

            _balances[mid] = GetCount(mid) + count;
            return new ItemAddResult(true, mid, count, count, null, Array.Empty<ConvertedReward>());
        }

        public bool ConsumeByInstance(string instanceId, int count) => false;
        public IItemInstance GetInstance(string instanceId) => null;
        public IReadOnlyList<IItemInstance> GetAll() => Array.Empty<IItemInstance>();
        public IReadOnlyList<IItemInstance> GetByCategory(ItemCategory category) => Array.Empty<IItemInstance>();
        public bool SetBound(string instanceId) => false;
        public int PurgeExpired() => 0;
    }
}
