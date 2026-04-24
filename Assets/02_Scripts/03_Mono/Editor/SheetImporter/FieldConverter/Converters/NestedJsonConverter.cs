#if UNITY_EDITOR
using System;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// 일반 POCO 클래스 폴백. 셀이 '{' 로 시작하면 JsonUtility.FromJson 으로 파싱.
    /// UnityEngine.Object / string 은 제외(다른 컨버터가 선점).
    /// </summary>
    public class NestedJsonConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType)
        {
            if (targetType == null) return false;
            if (!targetType.IsClass) return false;
            if (targetType == typeof(string)) return false;
            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType)) return false;
            return true;
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();
            if (!trimmed.StartsWith("{"))
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, "중첩 JSON 형식이 아닙니다('{' 로 시작해야 함).");
                return false;
            }

            try
            {
                value = JsonUtility.FromJson(trimmed, targetType);
                if (value == null)
                {
                    error = FieldConverterRegistry.FormatError(raw, targetType, "JsonUtility.FromJson 반환값 null.");
                    return false;
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, $"JsonUtility.FromJson 실패: {ex.Message}");
                return false;
            }
        }
    }
}
#endif
