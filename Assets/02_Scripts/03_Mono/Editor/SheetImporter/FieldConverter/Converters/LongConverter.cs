#if UNITY_EDITOR
using System;
using System.Globalization;

namespace PublicFramework.Editor.SheetImporter
{
    public class LongConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(long);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed))
            {
                value = parsed;
                error = null;
                return true;
            }

            value = 0L;
            error = FieldConverterRegistry.FormatError(raw, targetType, "long 변환 실패.");
            return false;
        }
    }
}
#endif
