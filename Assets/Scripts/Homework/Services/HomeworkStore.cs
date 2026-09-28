using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LoaInfo.Homework
{
    public class HomeworkStore
    {
        const string PrefsKey = "LoaInfo.Homework.Save";
        const string PrefsApiKey = "LoaInfo.Homework.ApiKey";

        static string LegacyFilePath =>
            Path.Combine(Application.persistentDataPath, "loa_homework.json");

        public HomeworkSaveData Data { get; private set; } = new HomeworkSaveData();

        public void Load()
        {
            try
            {
                if (PlayerPrefs.HasKey(PrefsKey))
                {
                    var json = PlayerPrefs.GetString(PrefsKey, "");
                    Data = Deserialize(json) ?? new HomeworkSaveData();
                }
                else if (File.Exists(LegacyFilePath))
                {
                    // 예전 파일 저장 → PlayerPrefs로 이전
                    var json = File.ReadAllText(LegacyFilePath);
                    Data = Deserialize(json) ?? new HomeworkSaveData();
                    Save();
                }
                else
                {
                    Data = new HomeworkSaveData();
                }

                if (Data.Characters == null)
                    Data.Characters = new List<CharacterHomework>();

                foreach (var c in Data.Characters)
                    HomeworkRules.MigrateCharacter(c);

                // API 키만 따로 둔 값도 병합 (혹시 모를 누락 대비)
                if (string.IsNullOrEmpty(Data.ApiKey) && PlayerPrefs.HasKey(PrefsApiKey))
                    Data.ApiKey = PlayerPrefs.GetString(PrefsApiKey, "");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HomeworkStore] Load failed: {e.Message}");
                Data = new HomeworkSaveData();
            }

            ApplyPendingResets();
        }

        public void Save()
        {
            try
            {
                if (Data.Characters == null)
                    Data.Characters = new List<CharacterHomework>();

                var json = Serialize(Data);
                PlayerPrefs.SetString(PrefsKey, json);
                PlayerPrefs.SetString(PrefsApiKey, Data.ApiKey ?? "");
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"[HomeworkStore] Save failed: {e.Message}");
            }
        }

        public void ApplyPendingResets()
        {
            var daily = LoaTime.GetDailyKey();
            var weekly = LoaTime.GetWeeklyKey();

            bool changed = false;

            if (!string.IsNullOrEmpty(Data.LastProcessedWeeklyKey) &&
                Data.LastProcessedWeeklyKey != weekly)
            {
                foreach (var c in Data.Characters)
                    HomeworkRules.ApplyWeeklyReset(c);
                changed = true;
            }

            if (!string.IsNullOrEmpty(Data.LastProcessedDailyKey) &&
                Data.LastProcessedDailyKey != daily)
            {
                int days = CountMissedDays(Data.LastProcessedDailyKey, daily);
                foreach (var c in Data.Characters)
                {
                    for (int i = 0; i < days; i++)
                        HomeworkRules.ApplyDailyReset(c, daily);
                }
                changed = true;
            }

            bool keysChanged =
                Data.LastProcessedDailyKey != daily ||
                Data.LastProcessedWeeklyKey != weekly;

            Data.LastProcessedDailyKey = daily;
            Data.LastProcessedWeeklyKey = weekly;

            if (changed || keysChanged)
                Save();
        }

        static int CountMissedDays(string fromKey, string toKey)
        {
            if (!DateTime.TryParse(fromKey, out var from) ||
                !DateTime.TryParse(toKey, out var to))
                return 1;

            var diff = (int)(to.Date - from.Date).TotalDays;
            return Math.Max(1, diff);
        }

        /// <summary>
        /// JsonUtility는 List 직렬화가 불안정해서 배열 래퍼 사용.
        /// </summary>
        [Serializable]
        class SaveDto
        {
            public string ApiKey = "";
            public string LastProcessedDailyKey = "";
            public string LastProcessedWeeklyKey = "";
            public CharacterHomework[] Characters = Array.Empty<CharacterHomework>();
            public int RecommendedRaidCount = 3;
            public bool ShowAllRaids;
            public bool ShowSingleMode = true;
        }

        static string Serialize(HomeworkSaveData data)
        {
            var dto = new SaveDto
            {
                ApiKey = data.ApiKey ?? "",
                LastProcessedDailyKey = data.LastProcessedDailyKey ?? "",
                LastProcessedWeeklyKey = data.LastProcessedWeeklyKey ?? "",
                Characters = data.Characters != null
                    ? data.Characters.ToArray()
                    : Array.Empty<CharacterHomework>(),
                RecommendedRaidCount = Math.Clamp(data.RecommendedRaidCount <= 0 ? 3 : data.RecommendedRaidCount, 1, 4),
                ShowAllRaids = data.ShowAllRaids,
                ShowSingleMode = data.ShowSingleMode
            };
            return JsonUtility.ToJson(dto);
        }

        static HomeworkSaveData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var dto = JsonUtility.FromJson<SaveDto>(json);
            if (dto == null) return null;

            // ShowSingleMode 기본 true — 구저장에 필드 없으면 false로 읽힐 수 있어 보정
            // JsonUtility는 bool 기본 false. 구버전 JSON에 키 없으면 false.
            // "ShowSingleMode" 문자열이 없으면 기본 true 유지
            bool showSingle = json.IndexOf("ShowSingleMode", StringComparison.Ordinal) >= 0
                ? dto.ShowSingleMode
                : true;

            int raidCount = dto.RecommendedRaidCount;
            if (raidCount <= 0) raidCount = 3;
            raidCount = Math.Clamp(raidCount, 1, 4);

            return new HomeworkSaveData
            {
                ApiKey = dto.ApiKey ?? "",
                LastProcessedDailyKey = dto.LastProcessedDailyKey ?? "",
                LastProcessedWeeklyKey = dto.LastProcessedWeeklyKey ?? "",
                Characters = dto.Characters != null
                    ? new List<CharacterHomework>(dto.Characters)
                    : new List<CharacterHomework>(),
                RecommendedRaidCount = raidCount,
                ShowAllRaids = dto.ShowAllRaids,
                ShowSingleMode = showSingle
            };
        }
    }
}
