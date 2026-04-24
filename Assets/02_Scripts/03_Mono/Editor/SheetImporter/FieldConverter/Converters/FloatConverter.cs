#if UNITY_EDITOR
using System;
using System.Globalization;

namespace PublicFramework.Editor.SheetImporter
{
    public class FloatConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(float);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed))
            {
                value = parsed;
                error = null;
                return true;
            }

            value = 0f;
            error = FieldConverterRegistry.FormatError(raw, targetType, "float 변환 실패.");
            return false;
        }
    }
}
#endif
