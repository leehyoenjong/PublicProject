using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IGachaRepository. 메모리 Dict 로 관리.</summary>
    public class FakeGachaRepository : IGachaRepository
    {
        private readonly Dictionary<string, IPityCounter> _counters = new Dictionary<string, IPityCounter>();
        private readonly Dictionary<(string gachaMID, PurchaseScope scope), int> _purchaseCounts = new Dictionary<(string, PurchaseScope), int>();
        private readonly List<string> _bannerEndedCalls = new List<string>();

        public IReadOnlyList<string> BannerEndedCalls => _bannerEndedCalls;

        public IReadOnlyList<IPityCounter> LoadAll()
        {
            var list = new List<IPityCounter>(_counters.Values);
            return list;
        }

        public void Save(IPityCounter counter)
        {
            if (counter == null) return;
            _counters[counter.GachaMID] = counter;
        }

        public void OnBannerEnded(string bannerMID, bool carryOver)
        {
            _bannerEndedCalls.Add($"{bannerMID}:{carryOver}");
        }

        public int GetPurchaseCount(string gachaMID, PurchaseScope scope)
        {
            return _purchaseCounts.TryGetValue((gachaMID, scope), out int c) ? c : 0;
        }

        public void SetPurchaseCount(string gachaMID, PurchaseScope scope, int count)
        {
            _purchaseCounts[(gachaMID, scope)] = count;
        }

        public void ResetPurchaseScope(PurchaseScope scope)
        {
            var keysToRemove = new List<(string, PurchaseScope)>();
            foreach (var kvp in _purchaseCounts)
            {
                if (kvp.Key.scope == scope) keysToRemove.Add(kvp.Key);
            }
            foreach (var k in keysToRemove) _purchaseCounts.Remove(k);
        }
    }
}
