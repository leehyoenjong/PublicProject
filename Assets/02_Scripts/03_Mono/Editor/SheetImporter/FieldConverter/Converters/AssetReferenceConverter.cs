#if UNITY_EDITOR
using System;
using UnityEditor;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>UnityEngine.Object 파생 타입. 입력이 "Assets/..."/"Packages/..." 경로 또는 32자 GUID 이면 해당 에셋 로드.</summary>
    public class AssetReferenceConverter : IFieldConverter
    {
        private const int GUID_LENGTH = 32;

        public bool CanConvert(Type targetType)
        {
            return targetType != null && typeof(UnityEngine.Object).IsAssignableFrom(targetType);
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();
            string path;

            if (trimmed.StartsWith("Assets/") || trimmed.StartsWith("Packages/"))
            {
                path = trimmed;
            }
            else if (IsGuid(trimmed))
            {
                path = AssetDatabase.GUIDToAssetPath(trimmed);
                if (string.IsNullOrEmpty(path))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, "GUID 해석 실패(대응 에셋 없음).");
                    return false;
                }
            }
            else
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, "경로('Assets/...') 또는 32자 GUID 이어야 합니다.");
                return false;
            }

            var asset = AssetDatabase.LoadAssetAtPath(path, targetType);
            if (asset == null)
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, $"경로 '{path}' 에서 {targetType.Name} 로드 실패.");
                return false;
            }

            value = asset;
            error = null;
            return true;
        }

        private static bool IsGuid(string s)
        {
            if (s.Length != GUID_LENGTH) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }
    }
}
#endif
