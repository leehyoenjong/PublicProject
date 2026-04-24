#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEditor;

namespace PublicFramework.Editor.SheetImporter
{
    /// <summary>
    /// DayOfWeekMask 전용 Converter.
    /// 시트 입력을 사람이 읽기 쉬운 형태로 받는다:
    ///   - 한국어 약자: 월 / 화 / 수 / 목 / 금 / 토 / 일
    ///   - 영문 3글자: Mon / Tue / Wed / Thu / Fri / Sat / Sun (대소문자 무시)
    ///   - 정수 비트마스크 (예: 21 = 월+수+금) — 기존 데이터 호환
    /// 구분자: ',' · '|' · 공백 · 탭. 빈 값 = None.
    /// EnumConverter 보다 먼저 등록되어 우선 시도된다.
    /// </summary>
    public class DayOfWeekMaskConverter : IFieldConverter
    {
        private static readonly char[] SEPARATORS = { ',', '|', ' ', '\t' };

        public bool CanConvert(Type targetType) => targetType == typeof(DayOfWeekMask);

        public bool TryConvert(string raw, Type targetType, out object value, out string error)
        {
            string trimmed = raw.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                value = DayOfWeekMask.None;
                error = null;
                return true;
            }

            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out long numeric))
            {
                value = (DayOfWeekMask)numeric;
                error = null;
                return true;
            }

            DayOfWeekMask result = DayOfWeekMask.None;
            var parts = trimmed.Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (string.IsNullOrEmpty(token)) continue;

                if (!TryMapToken(token, out var flag))
                {
                    value = DayOfWeekMask.None;
                    error = FieldConverterRegistry.FormatError(raw, targetType,
                        $"토큰 '{token}' 인식 실패. 허용: 월/화/수/목/금/토/일, Mon/Tue/Wed/Thu/Fri/Sat/Sun, 정수 비트마스크");
                    return false;
                }
                result |= flag;
            }

            value = result;
            error = null;
            return true;
        }

        private static bool TryMapToken(string token, out DayOfWeekMask flag)
        {
            switch (token)
            {
                case "월": flag = DayOfWeekMask.Mon; return true;
                case "화": flag = DayOfWeekMask.Tue; return true;
                case "수": flag = DayOfWeekMask.Wed; return true;
                case "목": flag = DayOfWeekMask.Thu; return true;
                case "금": flag = DayOfWeekMask.Fri; return true;
                case "토": flag = DayOfWeekMask.Sat; return true;
                case "일": flag = DayOfWeekMask.Sun; return true;
            }

            if (Enum.TryParse<DayOfWeekMask>(token, ignoreCase: true, out var parsed))
            {
                flag = parsed;
                return true;
            }

            flag = DayOfWeekMask.None;
            return false;
        }
    }

    [InitializeOnLoad]
    internal static class DayOfWeekMaskConverterRegistrar
    {
        static DayOfWeekMaskConverterRegistrar()
        {
            FieldConverterRegistry.Register(new DayOfWeekMaskConverter());
        }
    }
}
#endif
