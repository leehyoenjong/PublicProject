#if UNITY_EDITOR
using System;
using System.Globalization;

namespace PublicFramework.Editor.SheetImporter
{
    public class IntConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(int);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                value = parsed;
                error = null;
                return true;
            }

            value = 0;
            error = FieldConverterRegistry.FormatError(raw, targetType, "정수 변환 실패.");
            return false;
        }
    }
}
#endif
