using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LoaInfo.Homework
{
    /// <summary>
    /// 레이드 목록을 GitHub의 RemoteData/raids.json에서 받아 RaidCatalog에 적용.
    /// 받은 JSON은 PlayerPrefs에 캐시 → 다음 실행 시 오프라인이어도 최신 목록 사용.
    /// JSON의 version이 현재 적용된 목록보다 낮으면 무시(오래된 캐시/CDN이 새 목록을 덮지 않도록).
    /// 레이드 추가 시 raids.json의 version을 올리고, 앱 내장 목록을 바꿨다면 RaidCatalog.BuiltInVersion도 맞출 것.
    /// </summary>
    public static class RaidCatalogRemote
    {
        /// <summary>GitHub API (캐시가 거의 없어 푸시 즉시 반영). 비인증 IP당 시간당 60회 제한.</summary>
        const string ApiUrl =
            "https://api.github.com/repos/sioyou/LoaInfo/contents/RemoteData/raids.json?ref=main";

        /// <summary>API 실패(제한 초과 등) 시 대체. CDN 캐시로 최대 약 5분 늦게 반영.</summary>
        const string RawUrl =
            "https://raw.githubusercontent.com/sioyou/LoaInfo/main/RemoteData/raids.json";

        const string PrefsCacheKey = "LoaInfo.Homework.RaidCatalogCache";
        const int TimeoutSeconds = 10;

        [Serializable]
        class RaidEntryDto
        {
            public string id;
            public string group;
            public string name;
            public string difficulty;
            public int minItemLevel;
        }

        [Serializable]
        class RaidCatalogDto
        {
            public int version;
            public string updatedAt;
            public RaidEntryDto[] raids;
        }

        /// <summary>
        /// 캐시된 목록 적용. HomeworkStore.Load() 전에 호출해야
        /// MigrateCharacter가 원격에서 추가된 레이드를 지우지 않음.
        /// </summary>
        public static void LoadCached()
        {
            var json = PlayerPrefs.GetString(PrefsCacheKey, "");
            if (string.IsNullOrEmpty(json)) return;
            // 앱 업데이트로 내장 목록이 더 새로우면 캐시 폐기
            if (TryApply(json, out _) != ApplyResult.Applied)
                PlayerPrefs.DeleteKey(PrefsCacheKey);
        }

        /// <summary>원격 JSON 다운로드 → 적용 → 캐시. 반환: (변경 여부, 레이드 수, 오류)</summary>
        public static async Task<(bool changed, int count, string error)> FetchAsync()
        {
            var json = await DownloadAsync(ApiUrl, githubRaw: true)
                       ?? await DownloadAsync($"{RawUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}", githubRaw: false);
            if (json == null)
                return (false, 0, "레이드 목록 다운로드 실패");

            if (json == PlayerPrefs.GetString(PrefsCacheKey, ""))
                return (false, RaidCatalog.GetAll().Count, null);

            switch (TryApply(json, out var count))
            {
                case ApplyResult.Applied:
                    PlayerPrefs.SetString(PrefsCacheKey, json);
                    PlayerPrefs.Save();
                    return (true, count, null);
                case ApplyResult.Older:
                    // CDN 지연 등으로 받은 목록이 현재보다 오래됨 → 현재 유지
                    return (false, RaidCatalog.GetAll().Count, null);
                default:
                    return (false, 0, "레이드 목록 형식 오류");
            }
        }

        static async Task<string> DownloadAsync(string url, bool githubRaw)
        {
            using var req = UnityWebRequest.Get(url);
            req.timeout = TimeoutSeconds;
            if (githubRaw)
            {
                req.SetRequestHeader("Accept", "application/vnd.github.raw+json");
                req.SetRequestHeader("User-Agent", "LoaInfo");
            }

            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[RaidCatalogRemote] {url} 실패 ({req.responseCode}): {req.error}");
                return null;
            }
            return req.downloadHandler.text;
        }

        enum ApplyResult { Applied, Older, Invalid }

        static ApplyResult TryApply(string json, out int count)
        {
            count = 0;
            try
            {
                var dto = JsonUtility.FromJson<RaidCatalogDto>(json);
                if (dto?.raids == null) return ApplyResult.Invalid;
                if (dto.version < RaidCatalog.CurrentVersion) return ApplyResult.Older;

                var raids = dto.raids
                    .Where(r => r != null)
                    .Select(r => new RaidInfo
                    {
                        Id = r.id,
                        GroupKey = r.group,
                        Name = r.name,
                        Difficulty = r.difficulty ?? "",
                        MinItemLevel = r.minItemLevel
                    })
                    .ToList();

                if (!RaidCatalog.Apply(raids, dto.version)) return ApplyResult.Invalid;
                count = RaidCatalog.GetAll().Count;
                return ApplyResult.Applied;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RaidCatalogRemote] Parse failed: {e.Message}");
                return ApplyResult.Invalid;
            }
        }
    }
}
