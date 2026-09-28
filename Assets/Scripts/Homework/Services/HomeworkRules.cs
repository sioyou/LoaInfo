using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LoaInfo.Homework
{
    public static class HomeworkRules
    {
        public const float RestMax = 5f;
        public const float RestGain = 0.5f;
        public const float RestConsume = 1f;

        public static float ClampRest(float value)
        {
            if (value < 0f) return 0f;
            if (value > RestMax) return RestMax;
            return (float)Math.Round(value * 2f) / 2f;
        }

        public static void MigrateCharacter(CharacterHomework c)
        {
            if (c == null) return;

            if (c.CageRest <= 0f && c.GyunyeolRest > 0f)
                c.CageRest = c.GyunyeolRest;
            if (!c.CageDoneToday && c.GyunyeolDoneToday)
                c.CageDoneToday = true;
            if (!c.CageConsumedRestToday && c.GyunyeolConsumedRestToday)
                c.CageConsumedRestToday = true;

            if (c.Raids == null)
                c.Raids = Array.Empty<CharacterRaid>();

            // GroupKey 채우기 + 추적 대상(4막/종막/세르카/성당)만 유지 + 난이도 중복 제거
            var cleaned = new List<CharacterRaid>();
            foreach (var r in c.Raids.Where(x => x != null))
            {
                r.GroupKey = RaidCatalog.ResolveGroupKey(r);
                if (!RaidCatalog.IsTrackedGroup(r.GroupKey))
                    continue;

                var existing = cleaned.FindIndex(x => x.GroupKey == r.GroupKey);
                if (existing < 0)
                {
                    cleaned.Add(r);
                }
                else if (r.MinItemLevel >= cleaned[existing].MinItemLevel)
                {
                    r.DoneThisWeek = cleaned[existing].DoneThisWeek || r.DoneThisWeek;
                    cleaned[existing] = r;
                }
                else
                {
                    cleaned[existing].DoneThisWeek = cleaned[existing].DoneThisWeek || r.DoneThisWeek;
                }
            }
            c.Raids = cleaned.ToArray();
        }

        public static void ApplyDailyReset(CharacterHomework c, string newDailyKey)
        {
            if (!c.GatoDoneToday)
                c.GatoRest = ClampRest(c.GatoRest + RestGain);
            if (!c.CageDoneToday)
                c.CageRest = ClampRest(c.CageRest + RestGain);

            c.GatoDoneToday = false;
            c.CageDoneToday = false;
            c.GatoConsumedRestToday = false;
            c.CageConsumedRestToday = false;
        }

        public static void ApplyWeeklyReset(CharacterHomework c)
        {
            c.HalmosiDoneThisWeek = false;
            if (c.Raids == null) return;
            foreach (var r in c.Raids)
            {
                if (r != null) r.DoneThisWeek = false;
            }
        }

        /// <summary>할의 모래시계: 공식 가이드 기준 아이템 레벨 1730 이상</summary>
        public const int HalmosiMinItemLevel = 1730;

        public static bool CanDoHalmosi(CharacterHomework c)
        {
            if (c == null) return false;
            return TryParseItemLevel(c.ItemAvgLevel, out var lv) && lv + 0.5f >= HalmosiMinItemLevel;
        }

        public static void ToggleHalmosi(CharacterHomework c, out string message)
        {
            if (!CanDoHalmosi(c))
            {
                message = "할의 모래시계 입장 불가 (템렙 1730 필요)";
                return;
            }

            c.HalmosiDoneThisWeek = !c.HalmosiDoneThisWeek;
            message = c.HalmosiDoneThisWeek ? "할의 모래시계 완료" : "할의 모래시계 취소";
        }

        public static void ToggleDaily(CharacterHomework c, bool isGato, out string message)
        {
            if (isGato)
            {
                if (!c.GatoDoneToday)
                {
                    c.GatoDoneToday = true;
                    if (c.GatoRest >= RestConsume)
                    {
                        c.GatoRest = ClampRest(c.GatoRest - RestConsume);
                        c.GatoConsumedRestToday = true;
                        message = "가토 완료 · 휴게 1칸 소비";
                    }
                    else
                    {
                        c.GatoConsumedRestToday = false;
                        message = "가토 완료";
                    }
                }
                else
                {
                    c.GatoDoneToday = false;
                    if (c.GatoConsumedRestToday)
                    {
                        c.GatoRest = ClampRest(c.GatoRest + RestConsume);
                        c.GatoConsumedRestToday = false;
                    }
                    message = "가토 취소";
                }
            }
            else
            {
                if (!c.CageDoneToday)
                {
                    c.CageDoneToday = true;
                    if (c.CageRest >= RestConsume)
                    {
                        c.CageRest = ClampRest(c.CageRest - RestConsume);
                        c.CageConsumedRestToday = true;
                        message = "균열 완료 · 휴게 1칸 소비";
                    }
                    else
                    {
                        c.CageConsumedRestToday = false;
                        message = "균열 완료";
                    }
                }
                else
                {
                    c.CageDoneToday = false;
                    if (c.CageConsumedRestToday)
                    {
                        c.CageRest = ClampRest(c.CageRest + RestConsume);
                        c.CageConsumedRestToday = false;
                    }
                    message = "균열 취소";
                }
            }
        }

        public static void SetRest(CharacterHomework c, bool isGato, float value)
        {
            if (c == null) return;
            value = ClampRest(value);
            if (isGato) c.GatoRest = value;
            else c.CageRest = value;
        }

        public static void ToggleRaid(CharacterRaid raid, out string message)
        {
            raid.DoneThisWeek = !raid.DoneThisWeek;
            message = raid.DoneThisWeek
                ? $"{raid.DisplayName} 완료"
                : $"{raid.DisplayName} 취소";
        }

        /// <summary>
        /// 같은 레이드(GroupKey)는 난이도 1개만 유지. 선택 시 기존 난이도 교체.
        /// </summary>
        public static bool SetRaidDifficulty(CharacterHomework c, RaidInfo info, out string message)
        {
            message = null;
            if (c == null || info == null) return false;

            var list = c.Raids != null
                ? c.Raids.Where(r => r != null).ToList()
                : new List<CharacterRaid>();

            var idx = list.FindIndex(r => RaidCatalog.ResolveGroupKey(r) == info.GroupKey);
            bool wasDone = false;
            if (idx >= 0)
            {
                if (list[idx].RaidId == info.Id)
                {
                    message = "이미 선택된 난이도입니다";
                    return false;
                }
                wasDone = list[idx].DoneThisWeek;
                list.RemoveAt(idx);
            }

            list.Add(new CharacterRaid
            {
                RaidId = info.Id,
                GroupKey = info.GroupKey,
                DisplayName = info.DisplayName,
                MinItemLevel = info.MinItemLevel,
                DoneThisWeek = wasDone
            });

            // 입장 레벨 높은 순
            c.Raids = list
                .OrderByDescending(r => r.MinItemLevel)
                .ToArray();

            message = $"{info.DisplayName} 선택";
            return true;
        }

        public static void RemoveRaidGroup(CharacterHomework c, string groupKey)
        {
            if (c.Raids == null || string.IsNullOrEmpty(groupKey)) return;
            c.Raids = c.Raids
                .Where(r => r != null && RaidCatalog.ResolveGroupKey(r) != groupKey)
                .ToArray();
        }

        public static void RemoveRaid(CharacterHomework c, string raidId)
        {
            if (c.Raids == null) return;
            var info = RaidCatalog.Find(raidId);
            if (info != null)
                RemoveRaidGroup(c, info.GroupKey);
            else
                c.Raids = c.Raids.Where(r => r != null && r.RaidId != raidId).ToArray();
        }

        public static void EnsureRecommendedRaids(
            CharacterHomework c,
            int count = 3,
            bool showAll = false,
            bool showSingleMode = true)
        {
            if (!TryParseItemLevel(c.ItemAvgLevel, out var ilvl))
                return;

            if (c.Raids != null && c.Raids.Length > 0)
                return;

            foreach (var raid in RaidCatalog.GetRecommended(ilvl, count, showAll, showSingleMode))
                SetRaidDifficulty(c, raid, out _);
        }

        public static bool TryParseItemLevel(string raw, out float level)
        {
            level = 0f;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            // API 예: "1724.17", "1,724.17", "1.724,17"
            var s = raw.Trim().Replace(" ", "").Replace("\u00A0", "");

            if (s.Contains(',') && s.Contains('.'))
            {
                int lastComma = s.LastIndexOf(',');
                int lastDot = s.LastIndexOf('.');
                if (lastDot > lastComma)
                    s = s.Replace(",", "");          // 1,724.17
                else
                    s = s.Replace(".", "").Replace(',', '.'); // 1.724,17
            }
            else if (s.Contains(','))
            {
                var parts = s.Split(',');
                if (parts.Length == 2 && parts[1].Length <= 2)
                    s = parts[0] + "." + parts[1];   // 1724,17
                else
                    s = s.Replace(",", "");           // 1,724
            }

            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out level))
                return level > 0f;

            // 숫자만 추출 최후 시도
            var filtered = new System.Text.StringBuilder();
            bool dot = false;
            foreach (var ch in s)
            {
                if (char.IsDigit(ch)) filtered.Append(ch);
                else if (ch == '.' && !dot)
                {
                    filtered.Append(ch);
                    dot = true;
                }
            }

            return float.TryParse(filtered.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out level)
                   && level > 0f;
        }
    }
}
