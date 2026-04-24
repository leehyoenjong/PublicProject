using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IShopRepository. In-memory 저장.</summary>
    public class FakeShopRepository : IShopRepository
    {
        private readonly Dictionary<string, IShopProductInstance> _instances = new Dictionary<string, IShopProductInstance>();

        public int SaveCallCount { get; private set; }
        public int ResetScopeCallCount { get; private set; }
        public LimitScope? LastResetScope { get; private set; }

        public IReadOnlyList<IShopProductInstance> LoadAll()
        {
            return new List<IShopProductInstance>(_instances.Values).AsReadOnly();
        }

        public void Save(IShopProductInstance instance)
        {
            if (instance == null || string.IsNullOrEmpty(instance.ProductMID)) return;
            _instances[instance.ProductMID] = instance;
            SaveCallCount++;
        }

        public void ResetScope(LimitScope scope)
        {
            ResetScopeCallCount++;
            LastResetScope = scope;
        }

        public void Preload(IShopProductInstance instance)
        {
            if (instance != null) _instances[instance.ProductMID] = instance;
        }
    }
}
