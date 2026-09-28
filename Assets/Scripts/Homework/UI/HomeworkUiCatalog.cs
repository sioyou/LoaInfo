using TMPro;
using UnityEngine;

namespace LoaInfo.Homework.UI
{
    /// <summary>
    /// Resources/HomeworkUI/UiCatalog 에 저장되는 UI 프리팹 카탈로그.
    /// </summary>
    [CreateAssetMenu(menuName = "LoaInfo/Homework UI Catalog", fileName = "UiCatalog")]
    public class HomeworkUiCatalog : ScriptableObject
    {
        public TMP_FontAsset Font;
        public GameObject Text;
        public GameObject Button;
        public GameObject InputField;
        public GameObject Panel;
        public GameObject ListRow;
        public GameObject RestSlot;
        public GameObject ScrollView;
    }
}
