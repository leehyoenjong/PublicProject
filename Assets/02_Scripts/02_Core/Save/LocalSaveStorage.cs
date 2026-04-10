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
                Debug.Log($"[LocalSaveStorage] Written {data.Length} bytes to {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalSaveStorage] Write failed for slot {slotIndex}: {e}");
                throw;
            }
        }

        public byte[] Read(int slotIndex)
        {
            string path = SavePathHelper.GetSlotPath(slotIndex);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[LocalSaveStorage] File not found: {path}");
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(path);
                Debug.Log($"[LocalSaveStorage] Read {data.Length} bytes from {path}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalSaveStorage] Read failed for slot {slotIndex}: {e}");
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
                Debug.Log($"[LocalSaveStorage] Deleted: {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalSaveStorage] Delete failed for slot {slotIndex}: {e}");
            }
        }
    }
}
