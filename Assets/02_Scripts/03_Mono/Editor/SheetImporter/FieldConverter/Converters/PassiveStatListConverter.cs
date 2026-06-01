#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// `{StatType,StatLayer,Value},{StatType,StatLayer,Value}` 형식을 PassiveStat[] 로 변환.
    /// 공백 허용. enum 파싱은 대소문자 무관(Enum.TryParse ignoreCase=true).
    /// 예: `{Attack,Flat,10},{HP,Percent,0.05},{CritDamage,Multiplicative,1.5}`
    /// Value 단위 규약: Flat=절대값, Percent=비율(0.05 = +5%), Multiplicative=곱셈 인자(1.5 = ×1.5).
    /// 자세한 규약은 PassiveStat 주석 참고.
    /// </summary>
    public class PassiveStatListConverter : IFieldConverter
    {
        private static readonly Regex ENTRY_PATTERN = new Regex(
            @"\{\s*([A-Za-z_][A-Za-z0-9_]*)\s*,\s*([A-Za-z_][A-Za-z0-9_]*)\s*,\s*(-?\d+(?:\.\d+)?)\s*\}",
            RegexOptions.Compiled);

        public bool CanConvert(Type targetType)
        {
            if (targetType == null || !targetType.IsArray) return false;
            return targetType.GetElementType() == typeof(PassiveStat);
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            var matches = ENTRY_PATTERN.Matches(raw);
            if (matches.Count == 0)
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, "유효한 `{StatType,Layer,Value}` 항목이 없습니다.");
                return false;
            }

            var list = new List<PassiveStat>(matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                if (!Enum.TryParse(m.Groups[1].Value, true, out StatType statType))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"항목 {i} StatType 파싱 실패: '{m.Groups[1].Value}'");
                    return false;
                }
                if (!Enum.TryParse(m.Groups[2].Value, true, out StatLayer layer))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"항목 {i} StatLayer 파싱 실패: '{m.Groups[2].Value}'");
                    return false;
                }
                if (!float.TryParse(m.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"항목 {i} Value 파싱 실패: '{m.Groups[3].Value}'");
                    return false;
                }

                list.Add(new PassiveStat(statType, layer, v));
            }

            value = list.ToArray();
            error = null;
            return true;
        }
    }
}
#endif
