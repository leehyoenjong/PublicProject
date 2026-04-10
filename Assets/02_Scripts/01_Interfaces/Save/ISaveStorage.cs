namespace PublicFramework
{
    /// <summary>
    /// 세이브 데이터 저장소 인터페이스.
    /// 로컬 파일, 클라우드 등 저장 방식을 교체 가능.
    /// </summary>
    public interface ISaveStorage
    {
        void Write(int slotIndex, byte[] data);
        byte[] Read(int slotIndex);
        bool Exists(int slotIndex);
        void Delete(int slotIndex);
    }
}
