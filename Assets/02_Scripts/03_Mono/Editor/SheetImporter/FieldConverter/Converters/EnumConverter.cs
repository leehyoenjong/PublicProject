#if UNITY_EDITOR
using System;
using System.Globalization;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>Enum 이름 기반 변환. 정수 문자열도 지원(ToObject).</summary>
    public class EnumConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType != null && targetType.IsEnum;

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();

            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out long numeric))
            {
                try
                {
                    value = Enum.ToObject(targetType, numeric);
                    error = null;
                    return true;
                }
                catch (Exception ex)
                {
                    value = Activator.CreateInstance(targetType);
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"Enum.ToObject 실패: {ex.Message}");
                    return false;
                }
            }

            try
            {
                value = Enum.Parse(targetType, trimmed, ignoreCase: false);
                error = null;
                return true;
            }
            catch (Exception)
            {
                value = Activator.CreateInstance(targetType);
                error = FieldConverterRegistry.FormatError(raw, targetType, "enum 멤버명과 정확히 일치해야 함(대소문자 구분).");
                return false;
            }
        }
    }
}
#endif
