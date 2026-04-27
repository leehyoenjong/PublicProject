using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 IStatSystem. ownerId 별 FakeStatContainer 반환.</summary>
    public class FakeStatSystem : IStatSystem
    {
        private readonly Dictionary<string, FakeStatContainer> _containers = new();

        public int Count => _containers.Count;

        public FakeStatContainer GetOrCreate(string ownerId)
        {
            if (!_containers.TryGetValue(ownerId, out FakeStatContainer c))
            {
                c = new FakeStatContainer { OwnerId = ownerId };
                _containers[ownerId] = c;
            }
            return c;
        }

        public IStatContainer CreateContainer(string ownerId, int level = 1)
        {
            FakeStatContainer c = GetOrCreate(ownerId);
            c.SetLevel(level);
            return c;
        }

        public IStatContainer GetContainer(string ownerId) => GetOrCreate(ownerId);
        public bool RemoveContainer(string ownerId) => _containers.Remove(ownerId);

        public void TickAll(float deltaTime)
        {
            foreach (var c in _containers.Values) c.Tick(deltaTime);
        }
    }
}
