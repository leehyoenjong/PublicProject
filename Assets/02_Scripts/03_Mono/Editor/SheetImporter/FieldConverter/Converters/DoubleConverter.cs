#if UNITY_EDITOR
using System;
using System.Globalization;

namespace PublicFramework.Editor.SheetImporter
{
    public class DoubleConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(double);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
            {
                value = parsed;
                error = null;
                return true;
            }

            value = 0d;
            error = FieldConverterRegistry.FormatError(raw, targetType, "double 변환 실패.");
            return false;
        }
    }
}
#endif
