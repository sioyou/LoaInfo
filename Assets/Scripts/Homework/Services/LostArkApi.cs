using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LoaInfo.Homework
{
    public static class LostArkApi
    {
        const string BaseUrl = "https://developer-lostark.game.onstove.com";

        [Serializable]
        class SiblingListWrapper
        {
            public LostArkSibling[] items;
        }

        /// <summary>
        /// JsonUtility는 최상위 배열을 못 파싱하므로 감싸서 처리.
        /// 숫자형 ItemAvgLevel은 문자열로 정규화.
        /// </summary>
        public static async Task<(bool ok, List<LostArkSibling> siblings, string error)> FetchSiblingsAsync(
            string characterName,
            string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return (false, null, "API 키를 먼저 설정해주세요.");

            if (string.IsNullOrWhiteSpace(characterName))
                return (false, null, "캐릭터 닉네임을 입력해주세요.");

            var encoded = UnityWebRequest.EscapeURL(characterName.Trim());
            var url = $"{BaseUrl}/characters/{encoded}/siblings";

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("accept", "application/json");
            req.SetRequestHeader("authorization", $"bearer {apiKey.Trim()}");

            var op = req.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                var body = req.downloadHandler?.text;
                if (req.responseCode == 401 || req.responseCode == 403)
                    return (false, null, "API 키가 올바르지 않거나 권한이 없습니다.");
                if (req.responseCode == 404)
                    return (false, null, "캐릭터를 찾을 수 없습니다.");
                return (false, null, $"요청 실패 ({req.responseCode}): {req.error}\n{body}");
            }

            var text = req.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(text) || text == "null")
                return (false, null, "원정대 정보가 비어 있습니다.");

            try
            {
                text = NormalizeSiblingJson(text);
                var wrapped = "{\"items\":" + text + "}";
                var parsed = JsonUtility.FromJson<SiblingListWrapper>(wrapped);
                var list = new List<LostArkSibling>();
                if (parsed?.items != null)
                {
                    foreach (var s in parsed.items)
                    {
                        if (s != null && !string.IsNullOrEmpty(s.CharacterName))
                            list.Add(s);
                    }
                }

                list.Sort((a, b) =>
                {
                    HomeworkRules.TryParseItemLevel(b.ItemAvgLevel, out var lb);
                    HomeworkRules.TryParseItemLevel(a.ItemAvgLevel, out var la);
                    return lb.CompareTo(la);
                });

                if (list.Count == 0)
                    return (false, null, "원정대 캐릭터가 없습니다.");

                return (true, list, null);
            }
            catch (Exception e)
            {
                return (false, null, $"응답 파싱 실패: {e.Message}");
            }
        }

        static string NormalizeSiblingJson(string json)
        {
            // "ItemAvgLevel": 1724.17 → "ItemAvgLevel": "1724.17"
            return Regex.Replace(
                json,
                "\"ItemAvgLevel\"\\s*:\\s*([0-9]+(?:\\.[0-9]+)?)",
                "\"ItemAvgLevel\":\"$1\"",
                RegexOptions.CultureInvariant);
        }
    }
}
