using System.IO;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 세이브 파일 경로 유틸리티.
    /// </summary>
    public static class SavePathHelper
    {
        private const string SAVE_FOLDER = "SaveData";
        private const string SAVE_FILE_FORMAT = "save_slot_{0}.dat";

        public static string GetSaveDirectory()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public static string GetSlotPath(int slotIndex)
        {
            return Path.Combine(GetSaveDirectory(),
                string.Format(SAVE_FILE_FORMAT, slotIndex));
        }
    }
}
