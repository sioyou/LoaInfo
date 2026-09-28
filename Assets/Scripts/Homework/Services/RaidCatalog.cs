using System.Collections.Generic;
using System.Linq;

namespace LoaInfo.Homework
{
    /// <summary>
    /// 추적 레이드 목록. 기본값은 내장, RaidCatalogRemote가 원격 JSON으로 교체.
    /// 같은 GroupKey = 같은 레이드(난이도만 다름). 캐릭터당 그룹당 난이도 1개만 등록.
    /// </summary>
    public static class RaidCatalog
    {
        /// <summary>앱 내장 기본값. 원격/캐시 로드 실패 시 사용.</summary>
        static readonly RaidInfo[] BuiltIn =
        {
            R("belgardin_nm", "belgardin", "벨가르딘", "나이트메어", 1780),
            R("belgardin_hard", "belgardin", "벨가르딘", "하드", 1770),
            R("belgardin_normal", "belgardin", "벨가르딘", "노말", 1750),

            R("serka_nm", "serka", "세르카", "나이트메어", 1740),
            R("serka_hard", "serka", "세르카", "하드", 1730),
            R("serka_normal", "serka", "세르카", "노말", 1710),
            R("serka_single", "serka", "세르카", "싱글", 1710),

            R("cathedral_3", "cathedral", "지평의 성당", "3단계", 1750),
            R("cathedral_2", "cathedral", "지평의 성당", "2단계", 1720),
            R("cathedral_1", "cathedral", "지평의 성당", "1단계", 1700),
            R("cathedral_single", "cathedral", "지평의 성당", "싱글", 1700),

            R("kazeros_finale_hard", "kazeros_finale", "종막: 최후의 날", "하드", 1730),
            R("kazeros_finale_normal", "kazeros_finale", "종막: 최후의 날", "노말", 1710),
            R("kazeros_finale_single", "kazeros_finale", "종막: 최후의 날", "싱글", 1710),

            R("kazeros_act4_hard", "kazeros_act4", "4막: 파멸의 성채", "하드", 1720),
            R("kazeros_act4_normal", "kazeros_act4", "4막: 파멸의 성채", "노말", 1700),
            R("kazeros_act4_single", "kazeros_act4", "4막: 파멸의 성채", "싱글", 1700),
        };

        static RaidInfo R(string id, string group, string name, string diff, int lvl) =>
            new RaidInfo
            {
                Id = id,
                GroupKey = group,
                Name = name,
                Difficulty = diff,
                MinItemLevel = lvl
            };

        /// <summary>내장 목록 버전. RemoteData/raids.json의 version과 맞춰 관리.</summary>
        public const int BuiltInVersion = 2;

        static RaidInfo[] All = BuiltIn;

        /// <summary>현재 적용된 목록의 버전 (이보다 낮은 원격/캐시는 무시)</summary>
        public static int CurrentVersion { get; private set; } = BuiltInVersion;

        /// <summary>원격/캐시에서 받은 목록으로 교체. 유효 항목이 없으면 무시.</summary>
        public static bool Apply(IEnumerable<RaidInfo> raids, int version)
        {
            var list = raids?
                .Where(r => r != null &&
                    !string.IsNullOrEmpty(r.Id) &&
                    !string.IsNullOrEmpty(r.GroupKey) &&
                    !string.IsNullOrEmpty(r.Name))
                .GroupBy(r => r.Id)
                .Select(g => g.First())
                .ToArray();
            if (list == null || list.Length == 0) return false;
            All = list;
            CurrentVersion = version;
            return true;
        }

        /// <summary>설정 화면 안내용: "4막: 파멸의 성채 · 종막: 최후의 날 · ..."</summary>
        public static string GroupNamesText() =>
            string.Join(" · ", All
                .GroupBy(r => r.GroupKey)
                .OrderBy(g => g.Min(r => r.MinItemLevel))
                .Select(g => g.First().Name));

        public static IReadOnlyList<RaidInfo> GetAll() => All;

        public static RaidInfo Find(string id) =>
            All.FirstOrDefault(r => r.Id == id);

        public static string ResolveGroupKey(CharacterRaid raid)
        {
            if (raid == null) return "";
            if (!string.IsNullOrEmpty(raid.GroupKey)) return raid.GroupKey;
            var info = Find(raid.RaidId);
            if (info != null) return info.GroupKey;
            if (!string.IsNullOrEmpty(raid.DisplayName))
            {
                var bracket = raid.DisplayName.IndexOf('[');
                var name = bracket > 0 ? raid.DisplayName.Substring(0, bracket).Trim() : raid.DisplayName;
                var byName = All.FirstOrDefault(r => r.Name == name);
                if (byName != null) return byName.GroupKey;
            }
            return raid.RaidId ?? "";
        }

        public static bool IsTrackedGroup(string groupKey) =>
            All.Any(r => r.GroupKey == groupKey);

        /// <summary>입장 가능한 난이도만 포함한 레이드 그룹 목록</summary>
        public static List<RaidGroupView> GetAvailableGroups(
            float itemLevel,
            bool showAll = false,
            bool showSingleMode = true)
        {
            IEnumerable<RaidInfo> source = All;
            if (!showAll && itemLevel > 0f)
                source = All.Where(r => itemLevel + 0.5f >= r.MinItemLevel);

            if (!showSingleMode)
                source = source.Where(r => r.Difficulty == null ||
                    r.Difficulty.IndexOf("싱글", System.StringComparison.Ordinal) < 0);

            return source
                .GroupBy(r => r.GroupKey)
                .Select(g => new RaidGroupView
                {
                    GroupKey = g.Key,
                    Name = g.First().Name,
                    Difficulties = g.OrderBy(d => d.MinItemLevel).ThenBy(d => d.Difficulty).ToList()
                })
                .Where(g => g.Difficulties.Count > 0)
                .OrderByDescending(g => g.Difficulties.Max(d => d.MinItemLevel))
                .ToList();
        }

        public static List<RaidInfo> GetRecommended(
            float itemLevel,
            int count = 3,
            bool showAll = false,
            bool showSingleMode = true)
        {
            count = System.Math.Clamp(count, 1, 4);
            return GetAvailableGroups(itemLevel, showAll, showSingleMode)
                .Select(g =>
                {
                    var preferred = g.Difficulties
                        .Where(d => d.Difficulty == null ||
                            d.Difficulty.IndexOf("싱글", System.StringComparison.Ordinal) < 0)
                        .ToList();
                    var pool = preferred.Count > 0 ? preferred : g.Difficulties;
                    return pool.OrderByDescending(d => d.MinItemLevel).First();
                })
                .Take(count)
                .ToList();
        }
    }
}
