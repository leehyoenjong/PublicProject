using System.Collections.Generic;

namespace PublicFramework.Tests
{
    /// <summary>테스트용 ISaveStorage. in-memory dictionary + 호출 카운트.</summary>
    public class FakeSaveStorage : ISaveStorage
    {
        private readonly Dictionary<int, byte[]> _data = new Dictionary<int, byte[]>();
        public int WriteCalls { get; private set; }
        public int ReadCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public bool ThrowOnWrite { get; set; }

        public void Write(int slotIndex, byte[] data)
        {
            WriteCalls++;
            if (ThrowOnWrite) throw new System.IO.IOException("FakeSaveStorage.ThrowOnWrite");
            _data[slotIndex] = data;
        }

        public byte[] Read(int slotIndex)
        {
            ReadCalls++;
            return _data.TryGetValue(slotIndex, out byte[] bytes) ? bytes : null;
        }

        public bool Exists(int slotIndex) => _data.ContainsKey(slotIndex);

        public void Delete(int slotIndex)
        {
            DeleteCalls++;
            _data.Remove(slotIndex);
        }
    }
}
