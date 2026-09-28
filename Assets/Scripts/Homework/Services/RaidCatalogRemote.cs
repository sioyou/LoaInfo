using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LoaInfo.Homework
{
    /// <summary>
    /// 레이드 목록을 GitHub raw JSON에서 받아 RaidCatalog에 적용.
    /// 받은 JSON은 PlayerPrefs에 캐시 → 다음 실행 시 오프라인이어도 최신 목록 사용.
    /// 형식은 저장소의 RemoteData/raids.json 참고.
    /// </summary>
    public static class RaidCatalogRemote
    {
        /// <summary>raids.json의 raw URL. 비어 있으면 원격 로드 생략(내장 목록 사용).</summary>
        public const string RemoteUrl =
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

        static bool HasValidUrl =>
            !string.IsNullOrWhiteSpace(RemoteUrl) && !RemoteUrl.Contains("<OWNER>");

        /// <summary>
        /// 캐시된 목록 적용. HomeworkStore.Load() 전에 호출해야
        /// MigrateCharacter가 원격에서 추가된 레이드를 지우지 않음.
        /// </summary>
        public static void LoadCached()
        {
            var json = PlayerPrefs.GetString(PrefsCacheKey, "");
            if (string.IsNullOrEmpty(json)) return;
            if (!TryApply(json, out _))
                PlayerPrefs.DeleteKey(PrefsCacheKey);
        }

        /// <summary>원격 JSON 다운로드 → 적용 → 캐시. 반환: (변경 여부, 레이드 수, 오류)</summary>
        public static async Task<(bool changed, int count, string error)> FetchAsync()
        {
            if (!HasValidUrl)
                return (false, 0, "레이드 원격 URL 미설정");

            // 기기/중간 프록시 캐시 방지용. raw.githubusercontent.com CDN 자체는 쿼리를 무시하므로
            // 푸시 후 앱 반영까지 최대 약 5분 걸릴 수 있음.
            var url = $"{RemoteUrl}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            using var req = UnityWebRequest.Get(url);
            req.timeout = TimeoutSeconds;

            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
                return (false, 0, $"레이드 목록 다운로드 실패 ({req.responseCode})");

            var json = req.downloadHandler.text;
            var cached = PlayerPrefs.GetString(PrefsCacheKey, "");
            if (json == cached)
                return (false, RaidCatalog.GetAll().Count, null);

            if (!TryApply(json, out var count))
                return (false, 0, "레이드 목록 형식 오류");

            PlayerPrefs.SetString(PrefsCacheKey, json);
            PlayerPrefs.Save();
            return (true, count, null);
        }

        static bool TryApply(string json, out int count)
        {
            count = 0;
            try
            {
                var dto = JsonUtility.FromJson<RaidCatalogDto>(json);
                if (dto?.raids == null) return false;

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

                if (!RaidCatalog.Apply(raids)) return false;
                count = RaidCatalog.GetAll().Count;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RaidCatalogRemote] Parse failed: {e.Message}");
                return false;
            }
        }
    }
}
