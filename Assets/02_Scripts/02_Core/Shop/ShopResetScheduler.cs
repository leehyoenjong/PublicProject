using System;

namespace PublicFramework
{
    /// <summary>
    /// 상점 갱신 시각 계산 유틸. UTC 09:00 경계로 Daily/Weekly/Monthly 리셋.
    /// EventPeriod 는 시트에 기입된 eventStartUtc/eventEndUtc 문자열을 사용한다.
    /// IShopSystem 이 주기적으로 NextResetUtc 를 비교해 scope 리셋 판단.
    /// </summary>
    public static class ShopResetScheduler
    {
        /// <summary>일/주/월 갱신 기준 시각 (UTC).</summary>
        public const int RESET_HOUR_UTC = 9;

        /// <summary>
        /// 다음 리셋 시각 계산. ResetPeriod.None 은 DateTime.MaxValue 반환(갱신 없음).
        /// EventPeriod 는 eventEndUtc 파싱 결과 반환 — 종료 시각 = 최종 리셋 시각.
        /// </summary>
        public static DateTime GetNextResetUtc(IShopProduct product, DateTime nowUtc)
        {
            switch (product.ResetPeriod)
            {
                case ResetPeriod.Daily:
                    return GetNextDailyUtc(nowUtc);
                case ResetPeriod.Weekly:
                    return GetNextWeeklyUtc(nowUtc, product.WeeklyMask);
                case ResetPeriod.Monthly:
                    return GetNextMonthlyUtc(nowUtc);
                case ResetPeriod.EventPeriod:
                    return TryParseUtc(product.EventEndUtc, DateTime.MaxValue);
                case ResetPeriod.None:
                default:
                    return DateTime.MaxValue;
            }
        }

        /// <summary>EventPeriod 상품의 노출 가능 기간 체크.</summary>
        public static bool IsWithinEventPeriod(IShopProduct product, DateTime nowUtc)
        {
            if (product.ResetPeriod != ResetPeriod.EventPeriod) return true;

            DateTime start = TryParseUtc(product.EventStartUtc, DateTime.MinValue);
            DateTime end = TryParseUtc(product.EventEndUtc, DateTime.MaxValue);

            return nowUtc >= start && nowUtc < end;
        }

        /// <summary>
        /// ResetPeriod → LimitScope 매핑. 리셋 주기에 맞춰 scope 카운트를 비운다.
        /// Monthly/EventPeriod 는 Day 로 취급(플레이어 제한의 일일 리셋).
        /// </summary>
        public static LimitScope GetMatchingScope(ResetPeriod period)
        {
            switch (period)
            {
                case ResetPeriod.Weekly:
                    return LimitScope.Week;
                case ResetPeriod.Daily:
                case ResetPeriod.Monthly:
                case ResetPeriod.EventPeriod:
                default:
                    return LimitScope.Day;
            }
        }

        private static DateTime GetNextDailyUtc(DateTime nowUtc)
        {
            DateTime today = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, RESET_HOUR_UTC, 0, 0, DateTimeKind.Utc);
            return nowUtc < today ? today : today.AddDays(1);
        }

        private static DateTime GetNextWeeklyUtc(DateTime nowUtc, DayOfWeekMask mask)
        {
            if (mask == DayOfWeekMask.None)
            {
                return GetNextDailyUtc(nowUtc);
            }

            for (int offset = 0; offset < 8; offset++)
            {
                DateTime candidate = nowUtc.Date.AddDays(offset);
                DateTime resetUtc = new DateTime(candidate.Year, candidate.Month, candidate.Day, RESET_HOUR_UTC, 0, 0, DateTimeKind.Utc);

                if (resetUtc <= nowUtc) continue;
                if (!IsMaskMatch(candidate.DayOfWeek, mask)) continue;

                return resetUtc;
            }

            return DateTime.MaxValue;
        }

        private static DateTime GetNextMonthlyUtc(DateTime nowUtc)
        {
            DateTime firstOfThisMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, RESET_HOUR_UTC, 0, 0, DateTimeKind.Utc);
            return nowUtc < firstOfThisMonth ? firstOfThisMonth : firstOfThisMonth.AddMonths(1);
        }

        private static bool IsMaskMatch(DayOfWeek day, DayOfWeekMask mask)
        {
            DayOfWeekMask flag = ToMask(day);
            return (mask & flag) != 0;
        }

        private static DayOfWeekMask ToMask(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Monday: return DayOfWeekMask.Mon;
                case DayOfWeek.Tuesday: return DayOfWeekMask.Tue;
                case DayOfWeek.Wednesday: return DayOfWeekMask.Wed;
                case DayOfWeek.Thursday: return DayOfWeekMask.Thu;
                case DayOfWeek.Friday: return DayOfWeekMask.Fri;
                case DayOfWeek.Saturday: return DayOfWeekMask.Sat;
                case DayOfWeek.Sunday: return DayOfWeekMask.Sun;
                default: return DayOfWeekMask.None;
            }
        }

        private static DateTime TryParseUtc(string raw, DateTime fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback;

            if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed))
            {
                return parsed;
            }

            return fallback;
        }
    }
}
