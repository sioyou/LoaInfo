using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LoaInfo.Homework;

namespace LoaInfo.Homework.UI
{
    public static class UiTheme
    {
        public static readonly Color Bg = Hex("0E1016");
        public static readonly Color Panel = Hex("1A1D27");
        public static readonly Color PanelAlt = Hex("252A38");
        public static readonly Color Border = Hex("3A4258");
        public static readonly Color Text = Hex("F2F4FA");
        public static readonly Color TextDim = Hex("A8B0C4");
        public static readonly Color Accent = Hex("6B7CFF");
        public static readonly Color AccentSoft = Hex("3A3F78");
        public static readonly Color Selected = Hex("6B7CFF");
        public static readonly Color Done = Hex("2FCB7F");
        public static readonly Color Warn = Hex("F0B429");
        public static readonly Color Danger = Hex("E85D5D");
        public static readonly Color RestFill = Hex("8B7CFF");
        public static readonly Color SlotEmpty = Hex("2A3142");
        public static readonly Color SlotFilled = Hex("6B7CFF");
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.75f);

        public const float RowH = 72f;
        public const float BtnH = 64f;
        public const float HeaderH = 190f;
        public const float SummaryH = 132f;
        public const float CardPad = 16f;
        public const float TouchMin = 64f;
        public const float RestSlotW = 56f;
        public const float RestSlotH = 28f;

        public const int FontTitle = 30;
        public const int FontHead = 24;
        public const int FontBody = 20;
        public const int FontSmall = 16;
        public const int FontBtn = 20;

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var c);
            return c;
        }
    }

    public static class UiFactory
    {
        static HomeworkUiCatalog _catalog;
        static TMP_FontAsset _font;

        public static HomeworkUiCatalog Catalog
        {
            get
            {
                if (_catalog != null) return _catalog;
                _catalog = Resources.Load<HomeworkUiCatalog>("HomeworkUI/UiCatalog");
                return _catalog;
            }
        }

        public static TMP_FontAsset AppFont
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.Load<TMP_FontAsset>("Fonts/MaplestoryLight SDF");
                if (_font != null) return _font;

                if (Catalog != null && Catalog.Font != null)
                {
                    _font = Catalog.Font;
                    return _font;
                }

                var source = Resources.Load<Font>("Fonts/MaplestoryLight");
                if (source == null)
                    source = Resources.Load<Font>("Fonts/MaplestoryBold");
                if (source != null)
                {
                    _font = TMP_FontAsset.CreateFontAsset(source);
                    _font.name = "MaplestoryLight SDF (Runtime)";
                    return _font;
                }

                _font = TMP_Settings.defaultFontAsset;
                return _font;
            }
        }

        static GameObject Spawn(GameObject prefab, string name, Transform parent)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Object.Instantiate(prefab, parent, false);
                go.name = name;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        public static GameObject CreateRect(string name, Transform parent, Color? color = null)
        {
            GameObject go;
            if (Catalog != null && Catalog.Panel != null)
                go = Spawn(Catalog.Panel, name, parent);
            else
            {
                go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(parent, false);
            }

            if (go == null)
            {
                go = new GameObject(name, typeof(RectTransform));
                if (parent != null)
                    go.transform.SetParent(parent, false);
            }

            var img = go.GetComponent<Image>();
            if (color.HasValue)
            {
                if (img == null)
                    img = go.AddComponent<Image>();
                img.color = color.Value;
                img.raycastTarget = true;
                img.enabled = true;
            }
            else if (img != null)
            {
                // Destroy 하지 않음 (같은 프레임 AddComponent NRE 방지)
                img.enabled = false;
                img.raycastTarget = false;
            }

            return go;
        }

        public static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string content,
            int size,
            Color color,
            FontStyles style = FontStyles.Normal,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft,
            bool wrap = false)
        {
            var go = Spawn(Catalog != null ? Catalog.Text : null, name, parent);
            var text = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
            text.font = AppFont;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = style;
            text.alignment = align;
            text.enableWordWrapping = wrap;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        public static TMP_InputField CreateInput(string name, Transform parent, string placeholder)
        {
            var go = Spawn(Catalog != null ? Catalog.InputField : null, name, parent);
            var input = go.GetComponent<TMP_InputField>();
            if (input == null)
            {
                var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                img.color = UiTheme.PanelAlt;
                var text = CreateText("Text", go.transform, "", UiTheme.FontBody, UiTheme.Text);
                Stretch(text.rectTransform, 16, 12, 16, 12);
                var ph = CreateText("Placeholder", go.transform, placeholder, UiTheme.FontBody, UiTheme.TextDim, FontStyles.Italic);
                Stretch(ph.rectTransform, 16, 12, 16, 12);
                input = go.AddComponent<TMP_InputField>();
                input.textViewport = go.GetComponent<RectTransform>();
                input.textComponent = text;
                input.placeholder = ph;
            }
            else
            {
                if (input.placeholder is TMP_Text ph)
                    ph.text = placeholder;
                if (input.fontAsset == null)
                    input.fontAsset = AppFont;
                if (input.textComponent != null && input.textComponent.font == null)
                    input.textComponent.font = AppFont;
            }

            input.caretColor = UiTheme.Accent;
            input.selectionColor = UiTheme.AccentSoft;
            return input;
        }

        public static Button CreateButton(string name, Transform parent, string label, Color bg, Color fg, int fontSize = UiTheme.FontBtn)
        {
            var go = Spawn(Catalog != null ? Catalog.Button : null, name, parent);
            var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            img.color = bg;

            var btn = go.GetComponent<Button>() ?? go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.55f);
            btn.colors = colors;

            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = CreateText("Label", go.transform, label, fontSize, fg, FontStyles.Normal, TextAlignmentOptions.Center);
                Stretch(tmp.rectTransform, 8, 4, 8, 4);
            }
            else
            {
                tmp.font = AppFont;
                tmp.text = label;
                tmp.fontSize = fontSize;
                tmp.color = fg;
                tmp.fontStyle = FontStyles.Normal;
                tmp.alignment = TextAlignmentOptions.Center;
            }
            return btn;
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static LayoutElement LE(
            GameObject go,
            float? minH = null,
            float? preferredH = null,
            float? flexibleW = null,
            float? preferredW = null,
            float? minW = null,
            float? flexibleH = null)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (minH.HasValue) le.minHeight = minH.Value;
            if (preferredH.HasValue) le.preferredHeight = preferredH.Value;
            if (flexibleW.HasValue) le.flexibleWidth = flexibleW.Value;
            if (preferredW.HasValue) le.preferredWidth = preferredW.Value;
            if (minW.HasValue) le.minWidth = minW.Value;
            if (flexibleH.HasValue) le.flexibleHeight = flexibleH.Value;
            return le;
        }

        public static HorizontalLayoutGroup HRow(
            GameObject go,
            float spacing = 10,
            int padL = 12,
            int padR = 12,
            int padT = 8,
            int padB = 8)
        {
            var h = go.GetComponent<HorizontalLayoutGroup>() ?? go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padL, padR, padT, padB);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = true;
            return h;
        }

        public static HorizontalLayoutGroup HLayout(
            GameObject go,
            float spacing = 10,
            int padL = 0,
            int padR = 0,
            int padT = 0,
            int padB = 0) =>
            HRow(go, spacing, padL, padR, padT, padB);

        public static VerticalLayoutGroup VLayout(
            GameObject go,
            float spacing = 10,
            int padL = 0,
            int padR = 0,
            int padT = 0,
            int padB = 0,
            bool controlHeight = false,
            bool forceExpandHeight = false)
        {
            var v = go.GetComponent<VerticalLayoutGroup>() ?? go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padL, padR, padT, padB);
            v.childAlignment = TextAnchor.UpperLeft;
            v.childControlHeight = controlHeight;
            v.childControlWidth = true;
            v.childForceExpandHeight = forceExpandHeight;
            v.childForceExpandWidth = true;
            return v;
        }

        public static TextMeshProUGUI FlexText(
            string name,
            Transform parent,
            string content,
            int size,
            Color color,
            FontStyles style = FontStyles.Normal)
        {
            var t = CreateText(name, parent, content, size, color, style, TextAlignmentOptions.MidlineLeft, wrap: true);
            var le = LE(t.gameObject, preferredW: 0, flexibleW: 1, minW: 40, preferredH: 0);
            le.flexibleHeight = 1;
            return t;
        }

        public static Button FixedButton(
            string name,
            Transform parent,
            string label,
            Color bg,
            Color fg,
            float width,
            float height = UiTheme.BtnH,
            int fontSize = UiTheme.FontBtn)
        {
            var btn = CreateButton(name, parent, label, bg, fg, fontSize);
            var le = LE(btn.gameObject, preferredW: width, preferredH: height, minW: width, minH: height);
            le.flexibleWidth = 0;
            le.flexibleHeight = 0;
            return btn;
        }

        public static Button FlexButton(
            string name,
            Transform parent,
            string label,
            Color bg,
            Color fg,
            float height = UiTheme.BtnH,
            int fontSize = UiTheme.FontBtn)
        {
            var btn = CreateButton(name, parent, label, bg, fg, fontSize);
            var le = LE(btn.gameObject, preferredH: height, minH: height, preferredW: 0, flexibleW: 1, minW: 80);
            le.flexibleHeight = 0;
            return btn;
        }

        /// <summary>
        /// 휴게 게이지: 칸=1.0, 최대 5. 슬롯을 넓게 강조. 0.5면 반칸.
        /// </summary>
        public static void BuildRestGauge(Transform parent, float rest, Color fill, Color empty)
        {
            rest = Mathf.Clamp(rest, 0f, HomeworkRules.RestMax);
            int slots = Mathf.RoundToInt(HomeworkRules.RestMax);
            for (int i = 0; i < slots; i++)
            {
                float slotFill = Mathf.Clamp01(rest - i);
                var slotGo = Spawn(Catalog != null ? Catalog.RestSlot : null, $"R{i}", parent);
                var slotImg = slotGo.GetComponent<Image>() ?? slotGo.AddComponent<Image>();
                slotImg.color = empty;
                slotImg.raycastTarget = false;

                var le = LE(slotGo, preferredH: UiTheme.RestSlotH, minH: UiTheme.RestSlotH, preferredW: 0, minW: 20);
                le.flexibleWidth = 1f;

                var fillTr = slotGo.transform.Find("Fill");
                Image fillImg;
                RectTransform frt;
                if (fillTr != null)
                {
                    fillImg = fillTr.GetComponent<Image>() ?? fillTr.gameObject.AddComponent<Image>();
                    frt = fillTr.GetComponent<RectTransform>();
                }
                else
                {
                    var fillGo = CreateRect("Fill", slotGo.transform, fill);
                    fillImg = fillGo.GetComponent<Image>();
                    frt = fillGo.GetComponent<RectTransform>();
                }

                fillImg.color = fill;
                fillImg.raycastTarget = false;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = new Vector2(Mathf.Max(slotFill, 0.001f), 1f);
                frt.offsetMin = new Vector2(3, 3);
                frt.offsetMax = new Vector2(-3, -3);
                frt.pivot = new Vector2(0f, 0.5f);
                fillImg.enabled = slotFill > 0.001f;
            }
        }

        public static GameObject ListRow(string name, Transform parent, Color bg, float height = UiTheme.RowH)
        {
            var row = Spawn(Catalog != null ? Catalog.ListRow : null, name, parent);
            var img = row.GetComponent<Image>() ?? row.AddComponent<Image>();
            img.color = bg;
            var le = LE(row, preferredH: height, minH: height);
            le.flexibleHeight = 0;
            le.flexibleWidth = 1;
            if (row.GetComponent<HorizontalLayoutGroup>() == null)
                HRow(row, 10, 14, 14, 10, 10);

            // 프리팹에 기본 Label이 있으면 숨김 (호출측에서 텍스트를 다시 붙임)
            var builtIn = row.transform.Find("Label");
            if (builtIn != null)
                builtIn.gameObject.SetActive(false);

            return row;
        }

        public static ScrollRect CreateScrollView(string name, Transform parent, Color? bg = null)
        {
            var go = Spawn(Catalog != null ? Catalog.ScrollView : null, name, parent);
            var scroll = go.GetComponent<ScrollRect>();
            if (scroll == null)
            {
                var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
                img.color = bg ?? UiTheme.Bg;
                scroll = go.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.vertical = true;

                var viewport = CreateRect("Viewport", go.transform, bg ?? UiTheme.Bg);
                Stretch(viewport.GetComponent<RectTransform>());
                viewport.AddComponent<RectMask2D>();
                var content = CreateRect("Content", viewport.transform);
                var crt = content.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 1);
                crt.anchorMax = new Vector2(1, 1);
                crt.pivot = new Vector2(0.5f, 1);
                VLayout(content, 14, 0, 0, 0, 20, controlHeight: true, forceExpandHeight: false);
                var fitter = content.AddComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scroll.viewport = viewport.GetComponent<RectTransform>();
                scroll.content = crt;
            }

            if (bg.HasValue)
            {
                var img = go.GetComponent<Image>();
                if (img != null) img.color = bg.Value;
            }

            // 부드러운 스크롤 (벅벅임 완화)
            scroll.inertia = true;
            scroll.decelerationRate = 0.03f;
            scroll.scrollSensitivity = 25f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.elasticity = 0.05f;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            return scroll;
        }
    }

    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rt;
        Rect _last;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (_last != Screen.safeArea)
                Apply();
        }

        void Apply()
        {
            var safe = Screen.safeArea;
            _last = safe;
            if (_rt == null) return;

            var min = safe.position;
            var max = min + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
