#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>"r,g,b" 또는 "r,g,b,a" 형식, 0~1 float. 공백 허용.</summary>
    public class ColorConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(Color);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            var parts = raw.Split(',');
            if (parts.Length != 3 && parts.Length != 4)
            {
                value = Color.black;
                error = FieldConverterRegistry.FormatError(raw, targetType, "'r,g,b' 또는 'r,g,b,a' 형식이 아닙니다.");
                return false;
            }

            float[] v = new float[4] { 0f, 0f, 0f, 1f };
            for (int i = 0; i < parts.Length; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
                {
                    value = Color.black;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"요소[{i}] float 변환 실패.");
                    return false;
                }
            }

            value = new Color(v[0], v[1], v[2], v[3]);
            error = null;
            return true;
        }
    }
}
#endif
