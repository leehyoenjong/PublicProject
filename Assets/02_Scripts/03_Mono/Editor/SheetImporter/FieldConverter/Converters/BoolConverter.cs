#if UNITY_EDITOR
using System;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>TRUE/FALSE (대소문자 무시), 1/0 수용. 그 외 모두 실패.</summary>
    public class BoolConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(bool);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();

            if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
            {
                value = true;
                error = null;
                return true;
            }
            if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase) || trimmed == "0")
            {
                value = false;
                error = null;
                return true;
            }

            value = false;
            error = FieldConverterRegistry.FormatError(raw, targetType, "bool 변환 실패(TRUE/FALSE/1/0 만 허용).");
            return false;
        }
    }
}
#endif
