using System;
using System.IO;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 로컬 파일 시스템 기반 세이브 저장소 구현.
    /// </summary>
    public class LocalSaveStorage : ISaveStorage
    {
        public void Write(int slotIndex, byte[] data)
        {
            string path = SavePathHelper.GetSlotPath(slotIndex);
            try
            {
                File.WriteAllBytes(path, data);
                Debug.Log($"[세이브] {data.Length} 바이트 기록됨: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[세이브] 슬롯 {slotIndex} 쓰기 실패: {e}");
                throw;
            }
        }

        public byte[] Read(int slotIndex)
        {
            string path = SavePathHelper.GetSlotPath(slotIndex);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[세이브] 파일을 찾을 수 없음: {path}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                Debug.Log($"[세이브] {data.Length} 바이트 읽음: {path}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[세이브] 슬롯 {slotIndex} 읽기 실패: {e}");
                return null;
            }
        }

        public bool Exists(int slotIndex)
        {
            return File.Exists(SavePathHelper.GetSlotPath(slotIndex));
        }

        public void Delete(int slotIndex)
        {
            string path = SavePathHelper.GetSlotPath(slotIndex);
            if (!File.Exists(path))
                return;

            try
            {
                File.Delete(path);
                Debug.Log($"[세이브] 삭제됨: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[세이브] 슬롯 {slotIndex} 삭제 실패: {e}");
            }
        }
    }
}
