using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IStatSystem. ownerId 별 FakeStatContainer 반환.</summary>
    public class FakeStatSystem : IStatSystem
    {
        private readonly Dictionary<string, FakeStatContainer> _containers = new Dictionary<string, FakeStatContainer>();

        public FakeStatContainer GetOrCreate(string ownerId)
        {
            if (!_containers.TryGetValue(ownerId, out FakeStatContainer c))
            {
                c = new FakeStatContainer();
                _containers[ownerId] = c;
            }
            return c;
        }

        public IStatContainer CreateContainer(string ownerId) => GetOrCreate(ownerId);
        public IStatContainer GetContainer(string ownerId) => GetOrCreate(ownerId);
        public bool RemoveContainer(string ownerId) => _containers.Remove(ownerId);
    }
}
