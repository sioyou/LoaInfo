using System;

namespace LoaInfo.Homework
{
    /// <summary>
    /// 로아 시간: KST 기준. 일일 06:00, 주간 수요일 06:00.
    /// </summary>
    public static class LoaTime
    {
        static readonly TimeSpan KstOffset = TimeSpan.FromHours(9);

        public static DateTime NowKst => DateTime.UtcNow + KstOffset;

        /// <summary>현재 시각이 속한 게임 일일 키 (yyyy-MM-dd). 06시 이전이면 전날.</summary>
        public static string GetDailyKey(DateTime? kst = null)
        {
            var t = kst ?? NowKst;
            if (t.Hour < 6)
                t = t.Date.AddDays(-1);
            else
                t = t.Date;
            return t.ToString("yyyy-MM-dd");
        }

        /// <summary>수요일 06:00 기준 주간 키 (그 주의 수요일 날짜).</summary>
        public static string GetWeeklyKey(DateTime? kst = null)
        {
            var t = kst ?? NowKst;
            // 수요일 06시 이전이면 아직 지난주
            var adjusted = t.Hour < 6 ? t.AddDays(-1) : t;
            // DayOfWeek: Sunday=0 ... Wednesday=3
            int daysSinceWed = ((int)adjusted.DayOfWeek - (int)DayOfWeek.Wednesday + 7) % 7;
            var wed = adjusted.Date.AddDays(-daysSinceWed);
            return wed.ToString("yyyy-MM-dd");
        }

        public static string FormatResetInfo()
        {
            var now = NowKst;
            var daily = GetDailyKey(now);
            var weekly = GetWeeklyKey(now);
            return $"일일 {daily} · 주간(수) {weekly} · 지금 {now:MM/dd HH:mm}";
        }
    }
}
