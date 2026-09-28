using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LoaInfo.Homework.UI
{
    public class HomeworkApp : MonoBehaviour
    {
        HomeworkStore _store;
        TextMeshProUGUI _summaryText;
        TextMeshProUGUI _statusText;
        TextMeshProUGUI _resetInfoText;
        Transform _listContent;

        GameObject _modalRoot;
        GameObject _apiModal;
        GameObject _searchModal;
        GameObject _raidModal;
        GameObject _settingsModal;
        GameObject _restModal;
        TMP_InputField _apiInput;
        TMP_InputField _nickInput;
        Transform _siblingList;
        Transform _raidPickList;
        TextMeshProUGUI _raidModalTitle;
        TextMeshProUGUI _restTitleText;
        TextMeshProUGUI _restValueText;
        TextMeshProUGUI _settingsRaidCountText;
        TextMeshProUGUI _settingsSingleLabel;
        TextMeshProUGUI _settingsRaidListText;

        CharacterHomework _raidTarget;
        CharacterHomework _restTarget;
        bool _restIsGato;
        float _restEditValue;
        readonly List<LostArkSibling> _pendingSiblings = new();
        readonly HashSet<string> _selectedForAdd = new();
        bool _busy;

        void Awake()
        {
            // 캐시된 레이드 목록을 먼저 적용해야 Load 시 마이그레이션이 레이드를 지우지 않음
            RaidCatalogRemote.LoadCached();
            _store = new HomeworkStore();
            _store.Load();
            EnsureEventSystem();
            BuildUi();
            RefreshAll();
            _ = UpdateRaidCatalogAsync(silent: true);
        }

        /// <summary>원격 레이드 목록 갱신. silent면 실패/무변경 시 상태 메시지 생략.</summary>
        async Task<string> UpdateRaidCatalogAsync(bool silent)
        {
            var (changed, count, error) = await RaidCatalogRemote.FetchAsync();
            if (!changed)
            {
                if (!silent && error != null) SetStatus(error);
                return error;
            }

            foreach (var c in _store.Data.Characters)
                HomeworkRules.MigrateCharacter(c);
            _store.Save();
            RefreshAll();
            if (silent) SetStatus($"레이드 목록 업데이트됨 ({count}개)");
            return null;
        }

        void Update()
        {
            if (Time.frameCount % 120 != 0) return;

            var beforeD = _store.Data.LastProcessedDailyKey;
            var beforeW = _store.Data.LastProcessedWeeklyKey;
            _store.ApplyPendingResets();
            if (beforeD != _store.Data.LastProcessedDailyKey ||
                beforeW != _store.Data.LastProcessedWeeklyKey)
                RefreshAll();
            else if (_resetInfoText != null)
            {
                var info = LoaTime.FormatResetInfo();
                if (_resetInfoText.text != info)
                    _resetInfoText.text = info;
            }
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(es);
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("HomeworkCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvas.pixelPerfect = false;

            // 작은 reference → 폰에서 UI가 더 크게 보임
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1440);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.35f;
            scaler.referencePixelsPerUnit = 100f;

            var safe = UiFactory.CreateRect("SafeArea", canvasGo.transform, UiTheme.Bg);
            UiFactory.Stretch(safe.GetComponent<RectTransform>());
            safe.AddComponent<SafeAreaFitter>();

            var root = UiFactory.CreateRect("Root", safe.transform, UiTheme.Bg);
            UiFactory.Stretch(root.GetComponent<RectTransform>());
            UiFactory.VLayout(root, 0, 0, 0, 0, 0, controlHeight: true, forceExpandHeight: false);

            BuildHeader(root.transform);
            BuildSummary(root.transform);
            BuildList(root.transform);
            BuildModals(canvasGo.transform);
        }

        void BuildHeader(Transform parent)
        {
            var bar = UiFactory.CreateRect("Header", parent, UiTheme.Panel);
            var headerLe = UiFactory.LE(bar, preferredH: UiTheme.HeaderH, minH: UiTheme.HeaderH);
            headerLe.flexibleHeight = 0;
            UiFactory.VLayout(bar, 10, 16, 16, 14, 14, controlHeight: true, forceExpandHeight: false);

            var title = UiFactory.CreateText("Title", bar.transform, "LoaInfo 숙제", UiTheme.FontTitle, UiTheme.Text, FontStyles.Normal);
            UiFactory.LE(title.gameObject, preferredH: 40, minH: 40).flexibleHeight = 0;

            _resetInfoText = UiFactory.CreateText("ResetInfo", bar.transform, "", UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(_resetInfoText.gameObject, preferredH: 28, minH: 24).flexibleHeight = 0;

            var actions = UiFactory.CreateRect("Actions", bar.transform);
            UiFactory.LE(actions, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(actions, 10, 0, 0, 0, 0);

            UiFactory.FlexButton("SettingsBtn", actions.transform, "설정", UiTheme.PanelAlt, UiTheme.Text)
                .onClick.AddListener(OpenSettingsModal);
            UiFactory.FlexButton("ApiBtn", actions.transform, "API", UiTheme.PanelAlt, UiTheme.Text)
                .onClick.AddListener(OpenApiModal);
            UiFactory.FlexButton("RefreshBtn", actions.transform, "갱신", UiTheme.AccentSoft, UiTheme.Text)
                .onClick.AddListener(() => _ = RefreshCharactersAsync());
            UiFactory.FlexButton("AddBtn", actions.transform, "+ 캐릭터", UiTheme.Accent, Color.white)
                .onClick.AddListener(OpenSearchModal);
        }

        void BuildSummary(Transform parent)
        {
            var box = UiFactory.CreateRect("Summary", parent, UiTheme.PanelAlt);
            var summaryLe = UiFactory.LE(box, preferredH: UiTheme.SummaryH, minH: UiTheme.SummaryH);
            summaryLe.flexibleHeight = 0;
            UiFactory.VLayout(box, 8, 16, 16, 14, 14, controlHeight: true, forceExpandHeight: false);

            _summaryText = UiFactory.CreateText("SummaryText", box.transform, "", UiTheme.FontBody, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, wrap: true);
            var s1 = UiFactory.LE(_summaryText.gameObject, preferredH: 44, minH: 40);
            s1.flexibleHeight = 0;

            _statusText = UiFactory.CreateText("Status", box.transform,
                "캐릭터를 추가하고 숙제를 탭하세요.", UiTheme.FontSmall, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            var s2 = UiFactory.LE(_statusText.gameObject, preferredH: 36, minH: 32);
            s2.flexibleHeight = 0;
        }

        void BuildList(Transform parent)
        {
            var listWrap = UiFactory.CreateRect("ListWrap", parent, UiTheme.Bg);
            var le = UiFactory.LE(listWrap);
            le.flexibleHeight = 1;
            le.minHeight = 120;

            var scroll = UiFactory.CreateScrollView("Scroll", listWrap.transform, UiTheme.Bg);
            UiFactory.Stretch(scroll.GetComponent<RectTransform>(), 12, 8, 12, 16);
            _listContent = scroll.content;
        }

        void BuildModals(Transform canvas)
        {
            _modalRoot = UiFactory.CreateRect("Modals", canvas);
            UiFactory.Stretch(_modalRoot.GetComponent<RectTransform>());
            _modalRoot.SetActive(false);

            var dim = UiFactory.CreateRect("Dim", _modalRoot.transform, UiTheme.Overlay);
            UiFactory.Stretch(dim.GetComponent<RectTransform>());
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(CloseModals);

            _apiModal = BuildApiModal(_modalRoot.transform);
            _searchModal = BuildSearchModal(_modalRoot.transform);
            _raidModal = BuildRaidModal(_modalRoot.transform);
            _settingsModal = BuildSettingsModal(_modalRoot.transform);
            _restModal = BuildRestModal(_modalRoot.transform);
            _apiModal.SetActive(false);
            _searchModal.SetActive(false);
            _raidModal.SetActive(false);
            _settingsModal.SetActive(false);
            _restModal.SetActive(false);
        }

        GameObject BuildApiModal(Transform parent)
        {
            var panel = UiFactory.CreateRect("ApiModal", parent, UiTheme.Panel);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.06f, 0.22f);
            rt.anchorMax = new Vector2(0.94f, 0.78f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel, 14, 20, 20, 24, 24, controlHeight: true, forceExpandHeight: false);

            var t = UiFactory.CreateText("T", panel.transform, "로스트아크 API 키", UiTheme.FontHead, UiTheme.Text, FontStyles.Normal);
            UiFactory.LE(t.gameObject, preferredH: 40, minH: 40).flexibleHeight = 0;

            var hint = UiFactory.CreateText("Hint", panel.transform,
                "developer-lostark.game.onstove.com 에서 발급\nAuthorization bearer 토큰을 붙여넣으세요.",
                UiTheme.FontSmall, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.TopLeft, wrap: true);
            UiFactory.LE(hint.gameObject, preferredH: 56, minH: 48).flexibleHeight = 0;

            _apiInput = UiFactory.CreateInput("ApiInput", panel.transform, "API Key");
            UiFactory.LE(_apiInput.gameObject, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            _apiInput.contentType = TMP_InputField.ContentType.Password;

            var row = UiFactory.CreateRect("Row", panel.transform);
            UiFactory.LE(row, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(row, 12, 0, 0, 0, 0);

            UiFactory.FlexButton("Cancel", row.transform, "닫기", UiTheme.PanelAlt, UiTheme.Text)
                .onClick.AddListener(CloseModals);
            UiFactory.FlexButton("Save", row.transform, "저장", UiTheme.Accent, Color.white)
                .onClick.AddListener(() =>
                {
                    _store.Data.ApiKey = _apiInput.text?.Trim() ?? "";
                    _store.Save();
                    SetStatus(string.IsNullOrEmpty(_store.Data.ApiKey) ? "API 키가 비어 있습니다." : "API 키 저장됨");
                    CloseModals();
                });

            return panel;
        }

        GameObject BuildSearchModal(Transform parent)
        {
            var panel = UiFactory.CreateRect("SearchModal", parent, UiTheme.Panel);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0.03f);
            rt.anchorMax = new Vector2(0.97f, 0.97f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel, 12, 16, 16, 16, 16, controlHeight: true, forceExpandHeight: false);

            var title = UiFactory.CreateText("T", panel.transform, "원정대 캐릭터", UiTheme.FontHead, UiTheme.Text, FontStyles.Normal, wrap: true);
            UiFactory.LE(title.gameObject, preferredH: 36, minH: 36).flexibleHeight = 0;
            var hint = UiFactory.CreateText("Hint", panel.transform,
                "닉네임 검색 → 체크 후 추가. 등록된 캐릭터는 삭제 가능.", UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(hint.gameObject, preferredH: 40, minH: 32).flexibleHeight = 0;

            var searchRow = UiFactory.CreateRect("SearchRow", panel.transform);
            var searchLe = UiFactory.LE(searchRow, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH);
            searchLe.flexibleHeight = 0;
            UiFactory.HRow(searchRow, 10, 0, 0, 0, 0);

            _nickInput = UiFactory.CreateInput("Nick", searchRow.transform, "캐릭터 닉네임");
            UiFactory.LE(_nickInput.gameObject, preferredW: 0, flexibleW: 1, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH);

            UiFactory.FixedButton("Search", searchRow.transform, "검색", UiTheme.Accent, Color.white, 120)
                .onClick.AddListener(() => _ = SearchSiblingsAsync());

            var listWrap = UiFactory.CreateRect("SiblingWrap", panel.transform, UiTheme.PanelAlt);
            var lle = UiFactory.LE(listWrap);
            lle.flexibleHeight = 1;
            lle.minHeight = 200;

            var scroll = UiFactory.CreateScrollView("Scroll", listWrap.transform, UiTheme.PanelAlt);
            UiFactory.Stretch(scroll.GetComponent<RectTransform>(), 8, 8, 8, 8);
            var contentLayout = scroll.content.GetComponent<VerticalLayoutGroup>();
            if (contentLayout != null)
            {
                contentLayout.spacing = 10;
                contentLayout.padding = new RectOffset(6, 6, 6, 6);
            }
            _siblingList = scroll.content;

            // 모바일: 푸터 2단 (선택 / 추가)
            var footer = UiFactory.CreateRect("Footer", panel.transform);
            UiFactory.LE(footer, preferredH: UiTheme.BtnH * 2 + 10, minH: UiTheme.BtnH * 2 + 10).flexibleHeight = 0;
            UiFactory.VLayout(footer, 10, 0, 0, 0, 0, controlHeight: true, forceExpandHeight: false);

            var row1 = UiFactory.CreateRect("R1", footer.transform);
            UiFactory.LE(row1, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(row1, 10, 0, 0, 0, 0);
            UiFactory.FlexButton("SelectAll", row1.transform, "모두선택", UiTheme.Selected, Color.white)
                .onClick.AddListener(SelectAllPendingSiblings);
            UiFactory.FlexButton("DeselectAll", row1.transform, "모두해제", UiTheme.PanelAlt, UiTheme.Text)
                .onClick.AddListener(DeselectAllPendingSiblings);

            var row2 = UiFactory.CreateRect("R2", footer.transform);
            UiFactory.LE(row2, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(row2, 10, 0, 0, 0, 0);
            UiFactory.FlexButton("Close", row2.transform, "닫기", UiTheme.PanelAlt, UiTheme.Text)
                .onClick.AddListener(CloseModals);
            UiFactory.FlexButton("Add", row2.transform, "선택 추가", UiTheme.Done, Color.white)
                .onClick.AddListener(ConfirmAddCharacters);

            return panel;
        }

        GameObject BuildRaidModal(Transform parent)
        {
            var panel = UiFactory.CreateRect("RaidModal", parent, UiTheme.Panel);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0.04f);
            rt.anchorMax = new Vector2(0.97f, 0.96f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel, 12, 16, 16, 16, 16, controlHeight: true, forceExpandHeight: false);

            _raidModalTitle = UiFactory.CreateText("T", panel.transform, "레이드 추가", UiTheme.FontHead, UiTheme.Text, FontStyles.Normal, wrap: true);
            UiFactory.LE(_raidModalTitle.gameObject, preferredH: 36, minH: 36).flexibleHeight = 0;

            var hint = UiFactory.CreateText("Hint", panel.transform,
                "템렙으로 입장 가능한 레이드만 표시. 난이도는 레이드당 1개.", UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(hint.gameObject, preferredH: 40, minH: 32).flexibleHeight = 0;

            var listWrap = UiFactory.CreateRect("RaidWrap", panel.transform, UiTheme.PanelAlt);
            var lle = UiFactory.LE(listWrap);
            lle.flexibleHeight = 1;
            lle.minHeight = 200;

            var scroll = UiFactory.CreateScrollView("Scroll", listWrap.transform, UiTheme.PanelAlt);
            UiFactory.Stretch(scroll.GetComponent<RectTransform>(), 8, 8, 8, 8);
            var contentLayout = scroll.content.GetComponent<VerticalLayoutGroup>();
            if (contentLayout != null)
            {
                contentLayout.spacing = 10;
                contentLayout.padding = new RectOffset(6, 6, 6, 6);
            }
            _raidPickList = scroll.content;

            var footer = UiFactory.CreateRect("Footer", panel.transform);
            var footerLe = UiFactory.LE(footer, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH);
            footerLe.flexibleHeight = 0;
            UiFactory.HRow(footer, 10, 0, 0, 0, 0);

            UiFactory.FlexButton("Close", footer.transform, "완료", UiTheme.Accent, Color.white)
                .onClick.AddListener(() =>
                {
                    CloseModals();
                    RefreshAll();
                });

            return panel;
        }

        GameObject BuildSettingsModal(Transform parent)
        {
            var panel = UiFactory.CreateRect("SettingsModal", parent, UiTheme.Panel);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.06f, 0.18f);
            rt.anchorMax = new Vector2(0.94f, 0.82f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel, 14, 20, 20, 20, 20, controlHeight: true, forceExpandHeight: false);

            var t = UiFactory.CreateText("T", panel.transform, "숙제 설정", UiTheme.FontHead, UiTheme.Text, FontStyles.Normal);
            UiFactory.LE(t.gameObject, preferredH: 40, minH: 40).flexibleHeight = 0;

            _settingsRaidListText = UiFactory.CreateText("Hint", panel.transform, "",
                UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(_settingsRaidListText.gameObject, preferredH: 32, minH: 28).flexibleHeight = 0;

            // 추천 레이드 개수
            var countRow = UiFactory.CreateRect("CountRow", panel.transform, UiTheme.PanelAlt);
            UiFactory.LE(countRow, preferredH: 88, minH: 88).flexibleHeight = 0;
            UiFactory.HRow(countRow, 12, 14, 14, 12, 12);

            UiFactory.FlexText("CL", countRow.transform, "추천 레이드 개수", UiTheme.FontBody, UiTheme.Text);

            UiFactory.FixedButton("Down", countRow.transform, "▼", UiTheme.Panel, UiTheme.Text, 64, 64, UiTheme.FontHead)
                .onClick.AddListener(() =>
                {
                    _store.Data.RecommendedRaidCount = Mathf.Clamp(_store.Data.RecommendedRaidCount - 1, 1, 4);
                    RefreshSettingsLabels();
                });

            _settingsRaidCountText = UiFactory.CreateText("Count", countRow.transform, "3", UiTheme.FontTitle, UiTheme.Selected, FontStyles.Normal, TextAlignmentOptions.Center);
            UiFactory.LE(_settingsRaidCountText.gameObject, preferredW: 56, minW: 56, preferredH: 56, minH: 56).flexibleWidth = 0;

            UiFactory.FixedButton("Up", countRow.transform, "▲", UiTheme.Panel, UiTheme.Text, 64, 64, UiTheme.FontHead)
                .onClick.AddListener(() =>
                {
                    _store.Data.RecommendedRaidCount = Mathf.Clamp(_store.Data.RecommendedRaidCount + 1, 1, 4);
                    RefreshSettingsLabels();
                });

            // 싱글 모드
            var singleRow = UiFactory.CreateRect("SingleRow", panel.transform, UiTheme.PanelAlt);
            UiFactory.LE(singleRow, preferredH: UiTheme.RowH, minH: UiTheme.RowH).flexibleHeight = 0;
            UiFactory.HRow(singleRow, 10, 14, 14, 10, 10);
            _settingsSingleLabel = UiFactory.CreateText("SL", singleRow.transform, "", UiTheme.FontBody, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, wrap: true);
            UiFactory.LE(_settingsSingleLabel.gameObject, preferredW: 0, flexibleW: 1, minW: 80);
            var singleBtn = singleRow.AddComponent<Button>();
            singleBtn.transition = Selectable.Transition.ColorTint;
            singleBtn.onClick.AddListener(() =>
            {
                _store.Data.ShowSingleMode = !_store.Data.ShowSingleMode;
                RefreshSettingsLabels();
            });

            var tip = UiFactory.CreateText("Tip", panel.transform,
                "레이드 목록은 [갱신] 시 서버에서 최신으로 받아옵니다. 싱글 난이도 표시를 끌 수 있습니다.",
                UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(tip.gameObject, preferredH: 56, minH: 48).flexibleHeight = 0;

            _store.Data.ShowAllRaids = false;

            var footer = UiFactory.CreateRect("Footer", panel.transform);
            UiFactory.LE(footer, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(footer, 10, 0, 0, 0, 0);
            UiFactory.FlexButton("Save", footer.transform, "저장", UiTheme.Selected, Color.white)
                .onClick.AddListener(() =>
                {
                    _store.Save();
                    SetStatus("설정 저장됨");
                    CloseModals();
                });

            return panel;
        }

        GameObject BuildRestModal(Transform parent)
        {
            var panel = UiFactory.CreateRect("RestModal", parent, UiTheme.Panel);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.12f, 0.28f);
            rt.anchorMax = new Vector2(0.88f, 0.72f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            UiFactory.VLayout(panel, 14, 20, 20, 24, 24, controlHeight: true, forceExpandHeight: false);

            _restTitleText = UiFactory.CreateText("T", panel.transform, "휴게 수정", UiTheme.FontHead, UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.Center);
            UiFactory.LE(_restTitleText.gameObject, preferredH: 36, minH: 36).flexibleHeight = 0;

            var hint = UiFactory.CreateText("Hint", panel.transform, "▲▼ 0.5칸씩 · 1칸=1.0 · 최대 5칸", UiTheme.FontSmall, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
            UiFactory.LE(hint.gameObject, preferredH: 28, minH: 24).flexibleHeight = 0;

            var editor = UiFactory.CreateRect("Editor", panel.transform);
            UiFactory.LE(editor, preferredH: 100, minH: 100).flexibleHeight = 0;
            UiFactory.HRow(editor, 16, 0, 0, 0, 0).childAlignment = TextAnchor.MiddleCenter;

            UiFactory.FixedButton("Down", editor.transform, "▼", UiTheme.PanelAlt, UiTheme.Text, 88, 88, 36)
                .onClick.AddListener(() =>
                {
                    _restEditValue = HomeworkRules.ClampRest(_restEditValue - 0.5f);
                    RefreshRestValueLabel();
                });

            _restValueText = UiFactory.CreateText("Val", editor.transform, "0", 40, UiTheme.Selected, FontStyles.Normal, TextAlignmentOptions.Center);
            UiFactory.LE(_restValueText.gameObject, preferredW: 120, minW: 100, preferredH: 88, minH: 88).flexibleWidth = 0;

            UiFactory.FixedButton("Up", editor.transform, "▲", UiTheme.PanelAlt, UiTheme.Text, 88, 88, 36)
                .onClick.AddListener(() =>
                {
                    _restEditValue = HomeworkRules.ClampRest(_restEditValue + 0.5f);
                    RefreshRestValueLabel();
                });

            var footer = UiFactory.CreateRect("Footer", panel.transform);
            UiFactory.LE(footer, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(footer, 12, 0, 0, 0, 0);

            UiFactory.FlexButton("Cancel", footer.transform, "취소", UiTheme.PanelAlt, UiTheme.Text)
                .onClick.AddListener(CloseModals);
            UiFactory.FlexButton("Ok", footer.transform, "적용", UiTheme.Selected, Color.white)
                .onClick.AddListener(() =>
                {
                    if (_restTarget != null)
                    {
                        HomeworkRules.SetRest(_restTarget, _restIsGato, _restEditValue);
                        _store.Save();
                        var label = _restIsGato ? "가토" : "균열";
                        SetStatus($"{_restTarget.CharacterName} {label} 휴게 {_restEditValue:0.#}");
                        CloseModals();
                        RefreshAll();
                    }
                });

            return panel;
        }

        void RefreshSettingsLabels()
        {
            if (_settingsRaidListText != null)
                _settingsRaidListText.text = $"레이드: {RaidCatalog.GroupNamesText()}";

            if (_settingsRaidCountText != null)
                _settingsRaidCountText.text = Mathf.Clamp(_store.Data.RecommendedRaidCount, 1, 4).ToString();

            if (_settingsSingleLabel != null)
            {
                _settingsSingleLabel.text = "싱글 모드 난이도 표시";
                _settingsSingleLabel.color = _store.Data.ShowSingleMode ? Color.white : UiTheme.Text;
                var img = _settingsSingleLabel.transform.parent.GetComponent<Image>();
                if (img != null) img.color = _store.Data.ShowSingleMode ? UiTheme.Selected : UiTheme.PanelAlt;
            }
        }

        void RefreshRestValueLabel()
        {
            if (_restValueText != null)
                _restValueText.text = _restEditValue.ToString("0.#");
        }

        void OpenApiModal()
        {
            _modalRoot.SetActive(true);
            _apiModal.SetActive(true);
            _searchModal.SetActive(false);
            _raidModal.SetActive(false);
            _settingsModal.SetActive(false);
            _restModal.SetActive(false);
            _apiInput.text = _store.Data.ApiKey ?? "";
        }

        void OpenSettingsModal()
        {
            _modalRoot.SetActive(true);
            _apiModal.SetActive(false);
            _searchModal.SetActive(false);
            _raidModal.SetActive(false);
            _settingsModal.SetActive(true);
            _restModal.SetActive(false);
            if (_store.Data.RecommendedRaidCount <= 0)
                _store.Data.RecommendedRaidCount = 3;
            RefreshSettingsLabels();
        }

        void OpenRestModal(CharacterHomework c, bool isGato)
        {
            _restTarget = c;
            _restIsGato = isGato;
            _restEditValue = HomeworkRules.ClampRest(isGato ? c.GatoRest : c.CageRest);
            if (_restTitleText != null)
                _restTitleText.text = $"{c.CharacterName} · {(isGato ? "가토" : "균열")} 휴게";
            RefreshRestValueLabel();

            _modalRoot.SetActive(true);
            _apiModal.SetActive(false);
            _searchModal.SetActive(false);
            _raidModal.SetActive(false);
            _settingsModal.SetActive(false);
            _restModal.SetActive(true);
        }

        void OpenSearchModal()
        {
            if (string.IsNullOrWhiteSpace(_store.Data.ApiKey))
            {
                SetStatus("먼저 API 키를 설정해주세요.");
                OpenApiModal();
                return;
            }

            _modalRoot.SetActive(true);
            _apiModal.SetActive(false);
            _searchModal.SetActive(true);
            _raidModal.SetActive(false);
            _settingsModal.SetActive(false);
            _restModal.SetActive(false);
            _pendingSiblings.Clear();
            _selectedForAdd.Clear();
            ClearChildren(_siblingList);
            ShowTrackedCharactersInSearch();
        }

        void OpenRaidModal(CharacterHomework c)
        {
            HomeworkRules.MigrateCharacter(c);
            _raidTarget = c;
            bool parsed = HomeworkRules.TryParseItemLevel(c.ItemAvgLevel, out var ilvl);
            _raidModalTitle.text = parsed
                ? $"{c.CharacterName} · 레이드 선택 (템렙 {ilvl:0.##})"
                : $"{c.CharacterName} · 레이드 선택 (템렙 원본: {c.ItemAvgLevel})";

            ClearChildren(_raidPickList);
            var groups = RaidCatalog.GetAvailableGroups(
                parsed ? ilvl : 0f,
                _store.Data.ShowAllRaids,
                _store.Data.ShowSingleMode);
            var selectedByGroup = new Dictionary<string, string>();
            foreach (var r in c.Raids ?? System.Array.Empty<CharacterRaid>())
            {
                if (r == null) continue;
                selectedByGroup[RaidCatalog.ResolveGroupKey(r)] = r.RaidId;
            }

            if (!parsed)
            {
                var warn = UiFactory.CreateText("Warn", _raidPickList,
                    $"템렙 파싱 실패 → 전체 목록 표시 (원본: '{c.ItemAvgLevel}')",
                    UiTheme.FontSmall, UiTheme.Warn, wrap: true);
                UiFactory.LE(warn.gameObject, preferredH: 44, minH: 40).flexibleHeight = 0;
            }

            var tip = UiFactory.CreateText("Tip", _raidPickList,
                "같은 레이드는 난이도 1개만 선택. 탭해서 고르세요.",
                UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(tip.gameObject, preferredH: 40, minH: 36).flexibleHeight = 0;

            if (groups.Count == 0)
            {
                var empty = UiFactory.CreateText("Empty", _raidPickList,
                    $"입장 가능 레이드 없음 (파싱템렙 {ilvl:0.##})",
                    UiTheme.FontBody, UiTheme.TextDim, wrap: true);
                UiFactory.LE(empty.gameObject, preferredH: 56, minH: 48).flexibleHeight = 0;
            }
            else
            {
                foreach (var g in groups)
                {
                    selectedByGroup.TryGetValue(g.GroupKey, out var selectedId);
                    AddRaidGroupRow(g, selectedId);
                }
            }

            _modalRoot.SetActive(true);
            _apiModal.SetActive(false);
            _searchModal.SetActive(false);
            _raidModal.SetActive(true);
            _settingsModal.SetActive(false);
            _restModal.SetActive(false);
        }

        void AddRaidGroupRow(RaidGroupView group, string selectedRaidId)
        {
            bool hasSelection = !string.IsNullOrEmpty(selectedRaidId);
            float rowH = 44 + UiTheme.BtnH + 16;

            var box = UiFactory.CreateRect($"Group_{group.GroupKey}", _raidPickList,
                hasSelection ? UiTheme.Selected : UiTheme.Panel);
            var boxLe = UiFactory.LE(box, preferredH: rowH, minH: rowH);
            boxLe.flexibleHeight = 0;
            UiFactory.VLayout(box, 8, 14, 14, 12, 12, controlHeight: true, forceExpandHeight: false);

            var head = UiFactory.CreateRect("Head", box.transform);
            UiFactory.LE(head, preferredH: 36, minH: 36).flexibleHeight = 0;
            UiFactory.HRow(head, 8, 0, 0, 0, 0);

            UiFactory.FlexText("Name", head.transform, group.Name, UiTheme.FontBody, UiTheme.Text);

            if (hasSelection)
            {
                var gk = group.GroupKey;
                UiFactory.FixedButton("Clear", head.transform, "빼기", UiTheme.PanelAlt, UiTheme.Danger, 100, 44, UiTheme.FontSmall)
                    .onClick.AddListener(() =>
                    {
                        if (_raidTarget == null) return;
                        HomeworkRules.RemoveRaidGroup(_raidTarget, gk);
                        _store.Save();
                        OpenRaidModal(_raidTarget);
                        RefreshAll();
                        SetStatus($"{group.Name} 제거");
                    });
            }

            var diffs = UiFactory.CreateRect("Diffs", box.transform);
            UiFactory.LE(diffs, preferredH: UiTheme.BtnH - 8, minH: UiTheme.BtnH - 8).flexibleHeight = 0;
            UiFactory.HRow(diffs, 8, 0, 0, 0, 0);

            foreach (var d in group.Difficulties)
            {
                bool on = d.Id == selectedRaidId;
                float w = Mathf.Clamp(120 + d.Difficulty.Length * 6, 110, 180);
                var info = d;
                var btn = UiFactory.FixedButton(
                    d.Id,
                    diffs.transform,
                    $"{d.Difficulty}·{d.MinItemLevel}",
                    on ? UiTheme.Selected : UiTheme.PanelAlt,
                    on ? Color.white : UiTheme.Text,
                    w,
                    UiTheme.BtnH - 8,
                    UiTheme.FontSmall);
                btn.onClick.AddListener(() =>
                {
                    if (_raidTarget == null) return;
                    if (HomeworkRules.SetRaidDifficulty(_raidTarget, info, out var msg))
                    {
                        _store.Save();
                        OpenRaidModal(_raidTarget);
                        RefreshAll();
                        SetStatus($"{_raidTarget.CharacterName}: {msg}");
                    }
                    else
                    {
                        SetStatus(msg);
                    }
                });
            }
        }

        void CloseModals()
        {
            _modalRoot.SetActive(false);
            _apiModal.SetActive(false);
            _searchModal.SetActive(false);
            _raidModal.SetActive(false);
            _settingsModal.SetActive(false);
            _restModal.SetActive(false);
            _raidTarget = null;
            _restTarget = null;
        }

        void ShowTrackedCharactersInSearch()
        {
            if (_store.Data.Characters.Count == 0) return;

            var label = UiFactory.CreateText("TrackedLabel", _siblingList, "등록된 캐릭터", UiTheme.FontSmall, UiTheme.TextDim);
            UiFactory.LE(label.gameObject, preferredH: 32, minH: 32).flexibleHeight = 0;

            foreach (var c in _store.Data.Characters.ToList())
            {
                var row = UiFactory.ListRow($"Tracked_{c.CharacterName}", _siblingList, UiTheme.Panel);

                UiFactory.FlexText("Info", row.transform,
                    $"{c.CharacterName}\n{c.ServerName} · {c.ItemAvgLevel}",
                    UiTheme.FontBody, UiTheme.Text);

                var captured = c;
                UiFactory.FixedButton("Del", row.transform, "삭제", UiTheme.PanelAlt, UiTheme.Danger, 110, 52, UiTheme.FontSmall)
                    .onClick.AddListener(() =>
                    {
                        _store.Data.Characters.Remove(captured);
                        _store.Save();
                        ClearChildren(_siblingList);
                        ShowTrackedCharactersInSearch();
                        RefreshAll();
                        SetStatus($"{captured.CharacterName} 삭제됨");
                    });
            }
        }

        void SelectAllPendingSiblings()
        {
            if (_pendingSiblings.Count == 0)
            {
                SetStatus("먼저 원정대를 검색해주세요.");
                return;
            }

            var tracked = new HashSet<string>(_store.Data.Characters.Select(c => c.CharacterName));
            _selectedForAdd.Clear();
            foreach (var s in _pendingSiblings)
            {
                if (!tracked.Contains(s.CharacterName))
                    _selectedForAdd.Add(s.CharacterName);
            }

            RebuildPendingSiblingRows();
            SetStatus($"모두선택 · {_selectedForAdd.Count}명");
        }

        void DeselectAllPendingSiblings()
        {
            if (_pendingSiblings.Count == 0)
            {
                SetStatus("먼저 원정대를 검색해주세요.");
                return;
            }

            _selectedForAdd.Clear();
            RebuildPendingSiblingRows();
            SetStatus("모두해제");
        }

        void RebuildPendingSiblingRows()
        {
            ClearChildren(_siblingList);
            ShowTrackedCharactersInSearch();

            if (_pendingSiblings.Count == 0) return;

            var sep = UiFactory.CreateText("Sep", _siblingList, "원정대 검색 결과 (체크 후 추가)", UiTheme.FontSmall, UiTheme.TextDim);
            UiFactory.LE(sep.gameObject, preferredH: 32).flexibleHeight = 0;

            var tracked = new HashSet<string>(_store.Data.Characters.Select(c => c.CharacterName));
            foreach (var s in _pendingSiblings)
                AddSiblingRow(s, tracked.Contains(s.CharacterName));
        }

        async Task SearchSiblingsAsync()
        {
            if (_busy) return;
            _busy = true;
            SetStatus("원정대 조회 중...");
            try
            {
                var (ok, siblings, error) = await LostArkApi.FetchSiblingsAsync(_nickInput.text, _store.Data.ApiKey);
                _pendingSiblings.Clear();
                _selectedForAdd.Clear();

                if (!ok)
                {
                    ClearChildren(_siblingList);
                    ShowTrackedCharactersInSearch();
                    SetStatus(error);
                    var err = UiFactory.CreateText("Err", _siblingList, error, UiTheme.FontBody, UiTheme.Danger);
                    UiFactory.LE(err.gameObject, preferredH: 48).flexibleHeight = 0;
                    return;
                }

                _pendingSiblings.AddRange(siblings);
                var tracked = new HashSet<string>(_store.Data.Characters.Select(c => c.CharacterName));
                foreach (var s in siblings)
                {
                    if (!tracked.Contains(s.CharacterName))
                        _selectedForAdd.Add(s.CharacterName);
                }

                RebuildPendingSiblingRows();
                SetStatus($"원정대 {siblings.Count}명 조회됨");
            }
            finally
            {
                _busy = false;
            }
        }

        void AddSiblingRow(LostArkSibling s, bool alreadyTracked)
        {
            var row = UiFactory.ListRow($"Sib_{s.CharacterName}", _siblingList, UiTheme.Panel, UiTheme.RowH + 8);

            var toggleGo = UiFactory.CreateRect("Toggle", row.transform,
                alreadyTracked
                    ? UiTheme.Border
                    : (_selectedForAdd.Contains(s.CharacterName) ? UiTheme.Selected : UiTheme.SlotEmpty));
            var tLe = UiFactory.LE(toggleGo, preferredW: 48, minW: 48, preferredH: 48, minH: 48);
            tLe.flexibleWidth = 0;
            tLe.flexibleHeight = 0;

            UiFactory.FlexText("Info", row.transform,
                $"{s.CharacterName}\n{s.ServerName} · {s.CharacterClassName} · {s.ItemAvgLevel}",
                UiTheme.FontBody, alreadyTracked ? UiTheme.TextDim : UiTheme.Text);

            if (alreadyTracked)
            {
                var tag = UiFactory.CreateText("Tag", row.transform, "추가됨", UiTheme.FontSmall, UiTheme.Done, FontStyles.Normal, TextAlignmentOptions.Center);
                UiFactory.LE(tag.gameObject, preferredW: 80, minW: 72, preferredH: 40, minH: 40).flexibleWidth = 0;
            }
            else
            {
                var btn = row.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() =>
                {
                    if (_selectedForAdd.Contains(s.CharacterName))
                    {
                        _selectedForAdd.Remove(s.CharacterName);
                        toggleGo.GetComponent<Image>().color = UiTheme.SlotEmpty;
                    }
                    else
                    {
                        _selectedForAdd.Add(s.CharacterName);
                        toggleGo.GetComponent<Image>().color = UiTheme.Selected;
                    }
                });
            }
        }

        void ConfirmAddCharacters()
        {
            int added = 0;
            foreach (var s in _pendingSiblings)
            {
                if (!_selectedForAdd.Contains(s.CharacterName)) continue;
                if (_store.Data.Characters.Any(c => c.CharacterName == s.CharacterName)) continue;

                var ch = new CharacterHomework
                {
                    CharacterName = s.CharacterName,
                    ServerName = s.ServerName,
                    CharacterClassName = s.CharacterClassName,
                    ItemAvgLevel = s.ItemAvgLevel,
                    CharacterLevel = s.CharacterLevel,
                    Raids = System.Array.Empty<CharacterRaid>()
                };
                HomeworkRules.EnsureRecommendedRaids(
                    ch,
                    _store.Data.RecommendedRaidCount,
                    _store.Data.ShowAllRaids,
                    _store.Data.ShowSingleMode);
                _store.Data.Characters.Add(ch);
                added++;
            }

            _store.Data.Characters = _store.Data.Characters
                .OrderByDescending(c =>
                {
                    HomeworkRules.TryParseItemLevel(c.ItemAvgLevel, out var lv);
                    return lv;
                })
                .ToList();

            _store.Save();
            CloseModals();
            RefreshAll();
            SetStatus(added > 0
                ? $"{added}명 추가 · 추천 레이드 {_store.Data.RecommendedRaidCount}개 자동 등록"
                : "추가된 캐릭터가 없습니다");
        }

        /// <summary>
        /// 등록 캐릭터 정보(템렙/레벨/직업/서버) 갱신.
        /// siblings 1회 호출로 원정대 전체가 오므로, 아직 매칭 안 된 캐릭터로만 재조회.
        /// </summary>
        async Task RefreshCharactersAsync()
        {
            if (_busy) return;
            if (string.IsNullOrWhiteSpace(_store.Data.ApiKey))
            {
                SetStatus("먼저 API 키를 설정해주세요.");
                OpenApiModal();
                return;
            }
            if (_store.Data.Characters.Count == 0)
            {
                SetStatus("갱신할 캐릭터가 없습니다.");
                return;
            }

            _busy = true;
            SetStatus("캐릭터 정보 갱신 중...");
            try
            {
                var catalogError = await UpdateRaidCatalogAsync(silent: false);

                var pending = _store.Data.Characters.ToList();
                int updated = 0, levelChanged = 0;
                var failed = new List<string>();
                string lastError = null;

                while (pending.Count > 0)
                {
                    var probe = pending[0];
                    var (ok, siblings, error) = await LostArkApi.FetchSiblingsAsync(probe.CharacterName, _store.Data.ApiKey);
                    if (!ok)
                    {
                        pending.RemoveAt(0);
                        failed.Add(probe.CharacterName);
                        lastError = error;
                        continue;
                    }

                    var byName = siblings
                        .GroupBy(s => s.CharacterName)
                        .ToDictionary(g => g.Key, g => g.First());
                    foreach (var c in pending.ToList())
                    {
                        if (!byName.TryGetValue(c.CharacterName, out var s)) continue;
                        if (c.ItemAvgLevel != s.ItemAvgLevel) levelChanged++;
                        c.ServerName = s.ServerName;
                        c.CharacterClassName = s.CharacterClassName;
                        c.ItemAvgLevel = s.ItemAvgLevel;
                        c.CharacterLevel = s.CharacterLevel;
                        pending.Remove(c);
                        updated++;
                    }

                    // 조회 캐릭터 자신이 목록에 없으면(닉변 등) 무한루프 방지
                    if (pending.Contains(probe))
                    {
                        pending.Remove(probe);
                        failed.Add(probe.CharacterName);
                    }
                }

                _store.Data.Characters = _store.Data.Characters
                    .OrderByDescending(c =>
                    {
                        HomeworkRules.TryParseItemLevel(c.ItemAvgLevel, out var lv);
                        return lv;
                    })
                    .ToList();
                _store.Save();
                RefreshAll();

                var msg = $"{updated}명 갱신 · 템렙 변경 {levelChanged}명";
                if (failed.Count > 0)
                    msg += $"\n실패: {string.Join(", ", failed)}" + (lastError != null ? $" ({lastError})" : "");
                if (catalogError != null)
                    msg += $"\n{catalogError}";
                SetStatus(msg);
            }
            finally
            {
                _busy = false;
            }
        }

        void RefreshAll()
        {
            ClearChildren(_listContent);

            if (_store.Data.Characters.Count == 0)
            {
                var empty = UiFactory.CreateText("Empty", _listContent,
                    "아직 등록된 캐릭터가 없습니다.\n[+ 캐릭터]로 원정대를 불러오세요.",
                    UiTheme.FontBody, UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.Center);
                UiFactory.LE(empty.gameObject, preferredH: 140).flexibleHeight = 0;
            }
            else
            {
                foreach (var c in _store.Data.Characters)
                    BuildCharacterCard(c);
            }

            UpdateSummary();
            if (_resetInfoText != null)
                _resetInfoText.text = LoaTime.FormatResetInfo();
        }

        void BuildCharacterCard(CharacterHomework c)
        {
            HomeworkRules.MigrateCharacter(c);
            int raidCount = c.Raids?.Length ?? 0;

            var card = UiFactory.CreateRect($"Char_{c.CharacterName}", _listContent, UiTheme.Panel);
            UiFactory.LE(card).flexibleHeight = 0;
            UiFactory.VLayout(card, 12, 16, 16, 16, 16, controlHeight: true, forceExpandHeight: false);
            var csf = card.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 이름 + 삭제 (한 줄, 큰 터치)
            var head = UiFactory.CreateRect("Head", card.transform);
            UiFactory.LE(head, preferredH: 56, minH: 56).flexibleHeight = 0;
            UiFactory.HRow(head, 10, 0, 0, 0, 0);

            var name = UiFactory.CreateText("Name", head.transform, c.CharacterName, UiTheme.FontHead, UiTheme.Text, FontStyles.Normal);
            UiFactory.LE(name.gameObject, preferredW: 0, flexibleW: 1, minW: 80);

            var captured = c;
            UiFactory.FixedButton("Remove", head.transform, "삭제", UiTheme.PanelAlt, UiTheme.Danger, 110, 52, UiTheme.FontSmall)
                .onClick.AddListener(() =>
                {
                    _store.Data.Characters.Remove(captured);
                    _store.Save();
                    RefreshAll();
                    SetStatus($"{captured.CharacterName} 삭제됨");
                });

            var meta = UiFactory.CreateText("Meta", card.transform,
                $"{c.ServerName}  ·  {c.CharacterClassName}  ·  {c.ItemAvgLevel}",
                UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
            UiFactory.LE(meta.gameObject, preferredH: 28, minH: 24).flexibleHeight = 0;

            BuildDailyTapRow(card.transform, c);

            if (HomeworkRules.CanDoHalmosi(c))
                BuildHalmosiTapRow(card.transform, c);

            var raidHead = UiFactory.CreateRect("RaidHead", card.transform);
            UiFactory.LE(raidHead, preferredH: UiTheme.BtnH, minH: UiTheme.BtnH).flexibleHeight = 0;
            UiFactory.HRow(raidHead, 10, 0, 0, 0, 0);

            UiFactory.FlexText("RL", raidHead.transform,
                $"주간 레이드 {raidCount}", UiTheme.FontBody, UiTheme.TextDim);

            UiFactory.FixedButton("AddRaid", raidHead.transform, "+ 레이드", UiTheme.AccentSoft, UiTheme.Text, 150, 52, UiTheme.FontBody)
                .onClick.AddListener(() => OpenRaidModal(captured));

            if (raidCount == 0)
            {
                var none = UiFactory.CreateText("NoRaid", card.transform,
                    "레이드 없음 · + 레이드로 추가", UiTheme.FontSmall, UiTheme.TextDim, wrap: true);
                UiFactory.LE(none.gameObject, preferredH: 36, minH: 32).flexibleHeight = 0;
            }
            else
            {
                foreach (var raid in c.Raids)
                {
                    if (raid == null) continue;
                    BuildRaidTapRow(card.transform, c, raid);
                }
            }
        }

        void BuildDailyTapRow(Transform parent, CharacterHomework c)
        {
            var row = UiFactory.CreateRect("Daily", parent);
            UiFactory.LE(row, preferredH: 100, minH: 100).flexibleHeight = 0;
            UiFactory.HRow(row, 12, 0, 0, 0, 0);

            BuildDailyTap(row.transform, c, true);
            BuildDailyTap(row.transform, c, false);
        }

        void BuildDailyTap(Transform parent, CharacterHomework c, bool isGato)
        {
            var label = isGato ? "가토" : "균열";
            var done = isGato ? c.GatoDoneToday : c.CageDoneToday;
            var rest = isGato ? c.GatoRest : c.CageRest;

            var box = UiFactory.CreateRect(label, parent, done ? UiTheme.Selected : UiTheme.PanelAlt);
            UiFactory.LE(box, preferredH: 96, minH: 96, preferredW: 0, flexibleW: 1, minW: 100);
            UiFactory.VLayout(box, 6, 10, 10, 10, 10, controlHeight: true, forceExpandHeight: false);

            // 카드 전체 탭 → 완료/취소
            var boxImg = box.GetComponent<Image>();
            var boxBtn = box.AddComponent<Button>();
            boxBtn.targetGraphic = boxImg;
            boxBtn.transition = Selectable.Transition.ColorTint;
            var colors = boxBtn.colors;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            boxBtn.colors = colors;
            boxBtn.onClick.AddListener(() =>
            {
                HomeworkRules.ToggleDaily(c, isGato, out var msg);
                _store.Save();
                RefreshAll();
                SetStatus($"{c.CharacterName}: {msg}");
            });

            var title = UiFactory.CreateText("T", box.transform, label,
                UiTheme.FontHead, done ? Color.white : UiTheme.Text, FontStyles.Normal, TextAlignmentOptions.Center);
            UiFactory.LE(title.gameObject, preferredH: 34, minH: 32).flexibleHeight = 0;

            // 게이지만 별도 탭 → 휴게 수정 (자식 버튼이 부모 클릭을 가로챔)
            var restRow = UiFactory.CreateRect("RestRow", box.transform, done ? new Color(1, 1, 1, 0.18f) : UiTheme.SlotEmpty);
            UiFactory.LE(restRow, preferredH: 40, minH: 36).flexibleHeight = 0;
            var h = UiFactory.HRow(restRow, 6, 8, 8, 6, 6);
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childForceExpandWidth = true;

            var emptySlot = done ? new Color(1, 1, 1, 0.22f) : UiTheme.PanelAlt;
            UiFactory.BuildRestGauge(restRow.transform, rest, UiTheme.RestFill, emptySlot);

            var restImg = restRow.GetComponent<Image>();
            if (restImg != null) restImg.raycastTarget = true;
            var restBtn = restRow.AddComponent<Button>();
            restBtn.targetGraphic = restImg;
            restBtn.transition = Selectable.Transition.ColorTint;
            restBtn.onClick.AddListener(() => OpenRestModal(c, isGato));
        }

        void BuildHalmosiTapRow(Transform parent, CharacterHomework c)
        {
            var done = c.HalmosiDoneThisWeek;
            var row = UiFactory.ListRow("Halmosi", parent, done ? UiTheme.Selected : UiTheme.PanelAlt, UiTheme.RowH);

            UiFactory.FlexText("L", row.transform, "할의 모래시계",
                UiTheme.FontBody, done ? Color.white : UiTheme.Text);

            var tag = UiFactory.CreateText("Tag", row.transform, "주 1회", UiTheme.FontSmall,
                done ? new Color(1, 1, 1, 0.85f) : UiTheme.TextDim, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
            UiFactory.LE(tag.gameObject, preferredW: 72, minW: 64).flexibleWidth = 0;

            var btn = row.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                HomeworkRules.ToggleHalmosi(c, out var msg);
                _store.Save();
                RefreshAll();
                SetStatus($"{c.CharacterName}: {msg}");
            });
        }

        void BuildRaidTapRow(Transform parent, CharacterHomework c, CharacterRaid raid)
        {
            var row = UiFactory.ListRow($"Raid_{raid.RaidId}", parent,
                raid.DoneThisWeek ? UiTheme.Selected : UiTheme.PanelAlt, UiTheme.RowH);

            UiFactory.FlexText("L", row.transform, raid.DisplayName,
                UiTheme.FontBody, raid.DoneThisWeek ? Color.white : UiTheme.Text);

            var btn = row.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                HomeworkRules.ToggleRaid(raid, out var msg);
                _store.Save();
                RefreshAll();
                SetStatus($"{c.CharacterName}: {msg}");
            });
        }

        void UpdateSummary()
        {
            var chars = _store.Data.Characters;
            if (chars.Count == 0)
            {
                _summaryText.text = "등록된 캐릭터 없음";
                return;
            }

            int dailyLeft = chars.Count(c => !c.GatoDoneToday) + chars.Count(c => !c.CageDoneToday);
            int raidLeft = chars.Sum(c => (c.Raids ?? System.Array.Empty<CharacterRaid>())
                .Count(r => r != null && !r.DoneThisWeek));
            int raidDone = chars.Sum(c => (c.Raids ?? System.Array.Empty<CharacterRaid>())
                .Count(r => r != null && r.DoneThisWeek));
            int halmosiEligible = chars.Count(HomeworkRules.CanDoHalmosi);
            int halmosiLeft = chars.Count(c => HomeworkRules.CanDoHalmosi(c) && !c.HalmosiDoneThisWeek);

            _summaryText.text =
                $"일일 남음 {dailyLeft}\n레이드 {raidLeft}남음 / {raidDone}완료 · 할모시 {halmosiLeft}/{halmosiEligible}";
        }

        void SetStatus(string msg)
        {
            if (_statusText != null)
                _statusText.text = msg;
        }

        static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Destroy(t.GetChild(i).gameObject);
        }
    }
}
