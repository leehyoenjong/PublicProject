#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// ItemData.subtypeRef 처럼 필드 타입이 ScriptableObject 베이스이고 시트에는 int MID 가 들어오는 케이스를 처리.
    /// 값이 int 이면 프로젝트에서 '{MID}.asset' 을 검색해 IItemSubtypeInfo 구현 SO 로 로드한다.
    /// 값이 "Assets/..." 경로면 기존 경로 방식으로 로드한다.
    /// Registry 에 Insert(0) 로 등록되므로 AssetReferenceConverter 보다 먼저 시도된다.
    /// </summary>
    public class ItemSubtypeMIDConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType)
        {
            return targetType == typeof(ScriptableObject);
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();

            if (int.TryParse(trimmed, out int mid))
            {
                if (mid == 0)
                {
                    value = null;
                    error = null;
                    return true;
                }

                string[] guids = AssetDatabase.FindAssets($"{mid} t:ScriptableObject");
                string suffix = $"/{mid}.asset";
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!path.EndsWith(suffix, StringComparison.Ordinal)) continue;
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (so is IItemSubtypeInfo)
                    {
                        value = so;
                        error = null;
                        return true;
                    }
                }

                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType,
                    $"MID={mid} 에 대응하는 IItemSubtypeInfo SO 를 찾지 못했습니다 (검색 suffix='{suffix}').");
                return false;
            }

            if (trimmed.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(trimmed);
                if (asset is IItemSubtypeInfo)
                {
                    value = asset;
                    error = null;
                    return true;
                }

                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType,
                    $"경로 '{trimmed}' 에서 IItemSubtypeInfo SO 로드 실패.");
                return false;
            }

            value = null;
            error = FieldConverterRegistry.FormatError(raw, targetType,
                "정수 MID 또는 'Assets/...' 경로여야 합니다.");
            return false;
        }
    }

    [InitializeOnLoad]
    internal static class ItemSubtypeMIDConverterRegistrar
    {
        static ItemSubtypeMIDConverterRegistrar()
        {
            FieldConverterRegistry.Register(new ItemSubtypeMIDConverter());
        }
    }
}
#endif

