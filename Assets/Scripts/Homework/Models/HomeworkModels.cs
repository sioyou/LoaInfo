using System;
using System.Collections.Generic;

namespace LoaInfo.Homework
{
    [Serializable]
    public class LostArkSibling
    {
        public string ServerName;
        public string CharacterName;
        public int CharacterLevel;
        public string CharacterClassName;
        public string ItemAvgLevel;
    }

    [Serializable]
    public class CharacterRaid
    {
        public string RaidId;
        public string GroupKey;
        public string DisplayName;
        public int MinItemLevel;
        public bool DoneThisWeek;
    }

    [Serializable]
    public class CharacterHomework
    {
        public string CharacterName;
        public string ServerName;
        public string CharacterClassName;
        public string ItemAvgLevel;
        public int CharacterLevel;

        public float GatoRest;
        public float CageRest;

        public bool GatoDoneToday;
        public bool CageDoneToday;
        public bool GatoConsumedRestToday;
        public bool CageConsumedRestToday;

        public CharacterRaid[] Raids = Array.Empty<CharacterRaid>();

        /// <summary>할의 모래시계 주간 완료 (템렙 1730+ 자동 노출)</summary>
        public bool HalmosiDoneThisWeek;

        public float GyunyeolRest;
        public bool GyunyeolDoneToday;
        public bool GyunyeolConsumedRestToday;
    }

    [Serializable]
    public class HomeworkSaveData
    {
        public string ApiKey = "";
        public string LastProcessedDailyKey = "";
        public string LastProcessedWeeklyKey = "";
        public List<CharacterHomework> Characters = new List<CharacterHomework>();

        /// <summary>캐릭터 추가 시 자동 등록할 추천 레이드 개수 (1~6)</summary>
        public int RecommendedRaidCount = 3;

        /// <summary>true면 템렙 무시하고 카탈로그 전체 표시</summary>
        public bool ShowAllRaids;

        /// <summary>false면 난이도명에 '싱글' 포함 항목 숨김</summary>
        public bool ShowSingleMode = true;
    }

    public class RaidInfo
    {
        public string Id;
        public string GroupKey;
        public string Name;
        public string Difficulty;
        public int MinItemLevel;

        public string DisplayName => string.IsNullOrEmpty(Difficulty) ? Name : $"{Name} [{Difficulty}]";
    }

    public class RaidGroupView
    {
        public string GroupKey;
        public string Name;
        public List<RaidInfo> Difficulties = new List<RaidInfo>();
    }
}
