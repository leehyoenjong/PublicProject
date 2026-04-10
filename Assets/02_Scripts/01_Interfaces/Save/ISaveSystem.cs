using System;

namespace PublicFramework
{
    /// <summary>
    /// 세이브/로드 시스템 인터페이스.
    /// 슬롯 기반 저장, 키-값 데이터 관리.
    /// </summary>
    public interface ISaveSystem : IService
    {
        // 슬롯 관리
        SaveSlot[] GetAllSlots();
        SaveSlot GetSlot(int slotIndex);
        void DeleteSlot(int slotIndex);
        bool HasSlot(int slotIndex);

        // 데이터 저장/로드 (메모리)
        void Save<T>(int slotIndex, string key, T data);
        T Load<T>(int slotIndex, string key);
        bool HasKey(int slotIndex, string key);
        void DeleteKey(int slotIndex, string key);

        // 디스크 I/O
        void WriteToDisk(int slotIndex);
        void ReadFromDisk(int slotIndex);

        // 이벤트
        event Action<int> OnSaveCompleted;
        event Action<int> OnLoadCompleted;
        event Action<int, Exception> OnSaveFailed;
        event Action<int, Exception> OnLoadFailed;
    }
}
