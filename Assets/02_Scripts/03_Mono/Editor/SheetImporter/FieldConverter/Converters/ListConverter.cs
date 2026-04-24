#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>List&lt;T&gt; 변환. '[' 로 시작하면 JsonUtility 폴백, 아니면 ','/'|' 분리 후 재귀 변환.</summary>
    public class ListConverter : IFieldConverter
    {
        private static readonly char[] SEPARATORS = { ',', '|' };

        [Serializable]
        private class ListJsonWrapper<T>
        {
            public List<T> items;
        }

        public bool CanConvert(Type targetType)
        {
            return targetType != null
                && targetType.IsGenericType
                && targetType.GetGenericTypeDefinition() == typeof(List<>);
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            Type elementType = targetType.GetGenericArguments()[0];
            string trimmed = raw.Trim();

            if (trimmed.StartsWith("["))
            {
                return TryJsonParse(raw, trimmed, targetType, elementType, out value, out error);
            }

            var list = (IList)Activator.CreateInstance(targetType);
            var parts = trimmed.Split(SEPARATORS);
            for (int i = 0; i < parts.Length; i++)
            {
                if (!FieldConverterRegistry.TryConvert(parts[i], elementType, out var elem, out var elemErr))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"요소[{i}] 변환 실패: {elemErr}");
                    return false;
                }
                list.Add(elem);
            }

            value = list;
            error = null;
            return true;
        }

        private static bool TryJsonParse(string raw, string trimmed, Type targetType, Type elementType, out object value, out string error)
        {
            Type wrapperType = typeof(ListJsonWrapper<>).MakeGenericType(elementType);
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
                error = FieldConverterRegistry.FormatError(raw, targetType, $"JsonUtility 리스트 파싱 실패: {ex.Message}");
                return false;
            }
        }
    }
}
#endif
