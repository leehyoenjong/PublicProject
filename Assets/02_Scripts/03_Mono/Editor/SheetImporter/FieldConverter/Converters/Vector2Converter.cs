#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>"x,y" 형식. 공백 허용.</summary>
    public class Vector2Converter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(Vector2);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            var parts = raw.Split(',');
            if (parts.Length != 2)
            {
                value = Vector2.zero;
                error = FieldConverterRegistry.FormatError(raw, targetType, "'x,y' 형식이 아닙니다.");
                return false;
            }

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                value = Vector2.zero;
                error = FieldConverterRegistry.FormatError(raw, targetType, "요소 float 변환 실패.");
                return false;
            }

            value = new Vector2(x, y);
            error = null;
            return true;
        }
    }
}
#endif
