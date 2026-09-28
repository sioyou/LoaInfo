using UnityEngine;

namespace LoaInfo.Homework.UI
{
    /// <summary>
    /// SampleScene에 컴포넌트가 없어도 Play 시 자동으로 숙제 앱을 띄웁니다.
    /// </summary>
    public static class HomeworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<HomeworkApp>() != null)
                return;

            var go = new GameObject("LoaHomeworkApp");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<HomeworkApp>();
        }
    }
}
