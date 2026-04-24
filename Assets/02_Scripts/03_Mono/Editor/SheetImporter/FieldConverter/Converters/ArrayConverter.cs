#if UNITY_EDITOR
using System;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>T[] 변환. '[' 로 시작하면 JsonUtility 폴백, 아니면 ','/'|' 분리 후 재귀 변환.</summary>
    public class ArrayConverter : IFieldConverter
    {
        private static readonly char[] SEPARATORS = { ',', '|' };

        [Serializable]
        private class ArrayJsonWrapper<T>
        {
            public T[] items;
        }

        public bool CanConvert(Type targetType) => targetType != null && targetType.IsArray;

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            Type elementType = targetType.GetElementType();
            if (elementType == null)
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, "요소 타입을 얻지 못했습니다.");
                return false;
            }

            string trimmed = raw.Trim();

            if (trimmed.StartsWith("["))
            {
                return TryJsonParse(raw, trimmed, targetType, elementType, out value, out error);
            }

            var parts = trimmed.Split(SEPARATORS);
            Array result = Array.CreateInstance(elementType, parts.Length);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!FieldConverterRegistry.TryConvert(parts[i], elementType, out var elem, out var elemErr))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"요소[{i}] 변환 실패: {elemErr}");
                    return false;
                }
                result.SetValue(elem, i);
            }

            value = result;
            error = null;
            return true;
        }

        private static bool TryJsonParse(string raw, string trimmed, Type targetType, Type elementType, out object value, out string error)
        {
            Type wrapperType = typeof(ArrayJsonWrapper<>).MakeGenericType(elementType);
            string wrapped = "{\"items\":" + trimmed + "}";

            try
            {
                object wrapper = JsonUtility.FromJson(wrapped, wrapperType);
                if (wrapper == null)
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, "JsonUtility 반환값 null.");
                    return false;
                }
                var field = wrapperType.GetField("items");
                value = field.GetValue(wrapper);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, $"JsonUtility 배열 파싱 실패: {ex.Message}");
                return false;
            }
        }
    }
}
#endif
