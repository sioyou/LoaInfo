#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LoaInfo.Homework.UI.Editor
{
    /// <summary>
    /// MapleStory Light TMP 폰트 + Homework UI 프리팹/카탈로그 생성.
    /// 메뉴: LoaInfo → Build Homework UI Prefabs
    /// </summary>
    public static class HomeworkUiPrefabBuilder
    {
        const string PrefabDir = "Assets/Prefabs/HomeworkUI";
        const string CatalogPath = "Assets/Resources/HomeworkUI/UiCatalog.asset";
        const string FontAssetPath = "Assets/Resources/Fonts/MaplestoryLight SDF.asset";
        const string SourceFontPath = "Assets/Resources/Fonts/MaplestoryLight.ttf";
        const string LegacyBoldFontAsset = "Assets/Resources/Fonts/MaplestoryBold SDF.asset";

        [MenuItem("LoaInfo/Build Homework UI Prefabs")]
        public static void Build()
        {
            EnsureFolders();
            var font = BuildFontAsset();
            var catalog = AssetDatabase.LoadAssetAtPath<HomeworkUiCatalog>(CatalogPath);
            if (catalog == null)
                catalog = ScriptableObject.CreateInstance<HomeworkUiCatalog>();

            catalog.Font = font;
            catalog.Text = SavePrefab(BuildTextPrefab(font), $"{PrefabDir}/UI_Text.prefab");
            catalog.Button = SavePrefab(BuildButtonPrefab(font), $"{PrefabDir}/UI_Button.prefab");
            catalog.InputField = SavePrefab(BuildInputPrefab(font), $"{PrefabDir}/UI_InputField.prefab");
            catalog.Panel = SavePrefab(BuildPanelPrefab(), $"{PrefabDir}/UI_Panel.prefab");
            catalog.ListRow = SavePrefab(BuildListRowPrefab(font), $"{PrefabDir}/UI_ListRow.prefab");
            catalog.RestSlot = SavePrefab(BuildRestSlotPrefab(), $"{PrefabDir}/UI_RestSlot.prefab");
            catalog.ScrollView = SavePrefab(BuildScrollViewPrefab(), $"{PrefabDir}/UI_ScrollView.prefab");

            if (AssetDatabase.LoadAssetAtPath<HomeworkUiCatalog>(CatalogPath) == null)
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            else
                EditorUtility.SetDirty(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LoaInfo] Homework UI prefabs + MapleStory Light TMP catalog built.");
        }

        [InitializeOnLoadMethod]
        static void AutoBuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isPlaying) return;
                var catalog = AssetDatabase.LoadAssetAtPath<HomeworkUiCatalog>(CatalogPath);
                var lightFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
                if (catalog != null && lightFont != null && catalog.Font == lightFont)
                    return;
                if (!File.Exists(SourceFontPath) && !File.Exists("Assets/@Resources/Maplestory Light.ttf"))
                    return;
                try { Build(); }
                catch (System.Exception e) { Debug.LogWarning($"[LoaInfo] Auto prefab build skipped: {e.Message}"); }
            };
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(PrefabDir);
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Fonts");
            EnsureFolder("Assets/Resources/HomeworkUI");

            if (!File.Exists(SourceFontPath))
            {
                const string src = "Assets/@Resources/Maplestory Light.ttf";
                if (File.Exists(src))
                {
                    AssetDatabase.CopyAsset(src, SourceFontPath);
                    AssetDatabase.Refresh();
                }
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static TMP_FontAsset BuildFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null)
                throw new System.InvalidOperationException($"Font not found: {SourceFontPath}");

            var fontAsset = TMP_FontAsset.CreateFontAsset(source);
            fontAsset.name = "MaplestoryLight SDF";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            var mat = fontAsset.material;
            if (mat != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mat)))
                AssetDatabase.AddObjectToAsset(mat, fontAsset);

            if (fontAsset.atlasTexture != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(fontAsset.atlasTexture)))
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        static GameObject SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildTextPrefab(TMP_FontAsset font)
        {
            var go = new GameObject("UI_Text", typeof(RectTransform));
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 20;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.text = "텍스트";
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Truncate;
            return go;
        }

        static GameObject BuildButtonPrefab(TMP_FontAsset font)
        {
            var go = new GameObject("UI_Button", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.42f, 0.49f, 1f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.text = "버튼";
            Stretch(labelGo.GetComponent<RectTransform>(), 8, 4, 8, 4);
            return go;
        }

        static GameObject BuildInputPrefab(TMP_FontAsset font)
        {
            var go = new GameObject("UI_InputField", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.16f, 0.22f, 1f);
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.23f, 0.26f, 0.35f, 1f);
            outline.effectDistance = new Vector2(1, -1);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 20;
            text.color = Color.white;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            Stretch(textGo.GetComponent<RectTransform>(), 16, 12, 16, 12);

            var phGo = new GameObject("Placeholder", typeof(RectTransform));
            phGo.transform.SetParent(go.transform, false);
            var ph = phGo.AddComponent<TextMeshProUGUI>();
            ph.font = font;
            ph.fontSize = 20;
            ph.color = new Color(0.66f, 0.69f, 0.77f, 1f);
            ph.fontStyle = FontStyles.Normal;
            ph.alignment = TextAlignmentOptions.MidlineLeft;
            ph.raycastTarget = false;
            ph.text = "입력";
            Stretch(phGo.GetComponent<RectTransform>(), 16, 12, 16, 12);

            var input = go.AddComponent<TMP_InputField>();
            input.textViewport = go.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = ph;
            input.caretColor = new Color(0.42f, 0.49f, 1f, 1f);
            input.selectionColor = new Color(0.23f, 0.25f, 0.47f, 0.75f);
            input.fontAsset = font;
            return go;
        }

        static GameObject BuildPanelPrefab()
        {
            var go = new GameObject("UI_Panel", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.11f, 0.15f, 1f);
            img.raycastTarget = true;
            return go;
        }

        static GameObject BuildListRowPrefab(TMP_FontAsset font)
        {
            var go = new GameObject("UI_ListRow", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.16f, 0.22f, 1f);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 72;
            le.preferredHeight = 72;
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.padding = new RectOffset(14, 14, 10, 10);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;

            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            var lle = labelGo.AddComponent<LayoutElement>();
            lle.flexibleWidth = 1;
            lle.minWidth = 40;
            return go;
        }

        static GameObject BuildRestSlotPrefab()
        {
            var go = new GameObject("UI_RestSlot", typeof(RectTransform));
            var img = go.AddComponent<Image>();
            img.color = new Color(0.16f, 0.19f, 0.26f, 1f);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 56;
            le.minWidth = 48;
            le.preferredHeight = 28;
            le.minHeight = 28;
            le.flexibleWidth = 1;

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(go.transform, false);
            var fill = fillGo.AddComponent<Image>();
            fill.color = new Color(0.55f, 0.49f, 1f, 1f);
            fill.raycastTarget = false;
            var frt = fillGo.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = new Vector2(1f, 1f);
            frt.offsetMin = new Vector2(2, 2);
            frt.offsetMax = new Vector2(-2, -2);
            return go;
        }

        static GameObject BuildScrollViewPrefab()
        {
            var root = new GameObject("UI_ScrollView", typeof(RectTransform));
            var rootImg = root.AddComponent<Image>();
            rootImg.color = new Color(0.055f, 0.063f, 0.086f, 1f);
            rootImg.raycastTarget = true;

            var scroll = root.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.inertia = true;
            scroll.decelerationRate = 0.03f;
            scroll.scrollSensitivity = 25f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.elasticity = 0.05f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(root.transform, false);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(1, 1, 1, 0.01f);
            vpImg.raycastTarget = true;
            viewport.AddComponent<RectMask2D>();
            Stretch(viewport.GetComponent<RectTransform>());

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = Vector2.zero;

            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 14;
            v.padding = new RectOffset(0, 0, 0, 20);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = crt;
            return root;
        }

        static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
#endif
