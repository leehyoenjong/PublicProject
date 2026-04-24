#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// `{itemId,amount},{itemId,amount}` 형식을 QuestReward[] 로 변환.
    /// itemId 는 int(ItemData MID), amount 는 int. 공백 허용.
    /// </summary>
    public class RewardListConverter : IFieldConverter
    {
        private static readonly Regex ENTRY_PATTERN = new Regex(
            @"\{\s*(-?\d+)\s*,\s*(-?\d+)\s*\}",
            RegexOptions.Compiled);

        public bool CanConvert(Type targetType)
        {
            if (targetType == null || !targetType.IsArray) return false;
            return targetType.GetElementType() == typeof(QuestReward);
        }

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            var matches = ENTRY_PATTERN.Matches(raw);
            if (matches.Count == 0)
            {
                value = null;
                error = FieldConverterRegistry.FormatError(raw, targetType, "유효한 `{id,amount}` 항목이 없습니다.");
                return false;
            }

            var list = new List<QuestReward>(matches.Count);
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                if (!int.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ||
                    !int.TryParse(m.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount))
                {
                    value = null;
                    error = FieldConverterRegistry.FormatError(raw, targetType, $"항목 {i} 파싱 실패: '{m.Value}'");
                    return false;
                }

                list.Add(new QuestReward(id, amount));
            }

            value = list.ToArray();
            error = null;
            return true;
        }
    }
}
#endif
