#if UNITY_EDITOR
using System;

namespace PublicFramework.Editor.SheetImporter
{
    public class StringConverter : IFieldConverter
    {
        public bool CanConvert(Type targetType) => targetType == typeof(string);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            value = raw;
            error = null;
            return true;
        }
    }
}
#endif
