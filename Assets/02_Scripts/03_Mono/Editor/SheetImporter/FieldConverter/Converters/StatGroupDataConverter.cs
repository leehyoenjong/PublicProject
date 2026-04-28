#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 시트 셀에 StatGroupData.MID (string) 가 들어오고 필드 타입이 StatGroupData 인 케이스 처리.
    /// '{MID}.asset' 으로 검색해 StatGroupData SO 로 로드한다.
    /// "Assets/..." 경로도 허용. 값이 비어있으면 null 로 통과.
    /// </summary>
    public class StatGroupDataConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType)
        {
            return targetType == typeof(StatGroupData);
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                value = null;
                error = null;
                return true;
            }

            if (trimmed.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var asset = AssetDatabase.LoadAssetAtPath<StatGroupData>(trimmed);
                if (asset != null)
                {
                    value = asset;
                    error = null;
                    return true;
                }

                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType,
                    $"경로 '{trimmed}' 에서 StatGroupData 로드 실패.");
                return false;
            }

            string[] guids = AssetDatabase.FindAssets($"{trimmed} t:StatGroupData");
            string suffix = $"/{trimmed}.asset";
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!path.EndsWith(suffix, StringComparison.Ordinal)) continue;
                var so = AssetDatabase.LoadAssetAtPath<StatGroupData>(path);
                if (so != null)
                {
                    value = so;
                    error = null;
                    return true;
                }
            }

            value = null;
            error = FieldConverterRegistry.FormatError(raw, targetType,
                $"MID='{trimmed}' 에 대응하는 StatGroupData SO 를 찾지 못했습니다 (검색 suffix='{suffix}').");
            return false;
        }
    }

    [InitializeOnLoad]
    internal static class StatGroupDataConverterRegistrar
    {
        static StatGroupDataConverterRegistrar()
        {
            FieldConverterRegistry.Register(new StatGroupDataConverter());
        }
    }
}
#endif
