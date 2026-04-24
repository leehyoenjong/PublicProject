#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEngine;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>"x,y,z" 형식. 공백 허용.</summary>
    public class Vector3Converter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(Vector3);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            var parts = raw.Split(',');
            if (parts.Length != 3)
            {
                value = Vector3.zero;
                error = FieldConverterRegistry.FormatError(raw, targetType, "'x,y,z' 형식이 아닙니다.");
                return false;
            }

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
                !float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
                !float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                value = Vector3.zero;
                error = FieldConverterRegistry.FormatError(raw, targetType, "요소 float 변환 실패.");
                return false;
            }

            value = new Vector3(x, y, z);
            error = null;
            return true;
        }
    }
}
#endif
