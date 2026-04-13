using System;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// GDPR 동의 다이얼로그. 런타임 UGUI 조립 + 카테고리별 Toggle(Analytics/Marketing/Functional).
    /// Functional 은 고정 체크 + 비활성 (프레임워크 핵심 기능). Analytics/Marketing 은 사용자 선택.
    /// 결과는 <see cref="ConsentStore"/> 에 저장되고 <see cref="BackendConsentChangedEvent"/> 로 발행.
    /// </summary>
    public class BackendConsentDialog : MonoBehaviour
    {
        private const string CANVAS_NAME = "ConsentCanvas";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private string _titleText = "Consent";
        [SerializeField] private string _bodyText = "Please choose which data categories to allow.";
        [SerializeField] private string _requiredLabel = "Required (cannot be disabled — app cannot run if declined)";
        [SerializeField] private string _analyticsLabel = "Analytics (session & crash)";
        [SerializeField] private string _marketingLabel = "Marketing (advertising id)";
        [SerializeField] private string _functionalLabel = "Functional";
        [SerializeField] private string _agreeLabel = "Confirm";
        [SerializeField] private string _declineLabel = "Decline Optional";

        private BackendConfig _config;
        private IBackendAnalytics _analytics;
        private IEventBus _eventBus;

        private Toggle _requiredToggle;
        private Toggle _analyticsToggle;
        private Toggle _marketingToggle;
        private Toggle _functionalToggle;
        private Button _agreeButton;

        /// <summary>
        /// 저장된 동의 버전이 현재 Config.ConsentVersion 과 다르면 재동의 필요 (true).
        /// </summary>
        public static bool RequiresConsent(BackendConfig config)
        {
            if (config == null) return false;
            return ConsentStore.RequiresReshow(config.ConsentVersion);
        }

        public void Configure(BackendConfig config, IBackendAnalytics analytics, IEventBus eventBus)
        {
            _config = config;
            _analytics = analytics;
            _eventBus = eventBus;
        }

        public void Show(Action<bool> onResult)
        {
            var canvas = _canvas != null ? _canvas : CreateDefaultCanvas();
            BuildDialog(canvas, onResult);
        }

        private Canvas CreateDefaultCanvas()
        {
            var go = new GameObject(CANVAS_NAME);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void BuildDialog(Canvas canvas, Action<bool> onResult)
        {
            var panel = CreatePanel(canvas.transform);
            CreateText(panel.transform, _titleText, 24, new Vector2(0, 170));
            CreateText(panel.transform, _bodyText, 14, new Vector2(0, 125));

            // Required 는 기본 true + interactable=false. 사용자는 해제할 수 없고, 거부 시 앱 사용 불가(프로젝트 UX).
            _requiredToggle = CreateCheckbox(panel.transform, _requiredLabel, new Vector2(0, 75), true, false);
            _analyticsToggle = CreateCheckbox(panel.transform, _analyticsLabel, new Vector2(0, 40),
                ConsentStore.GetConsent(ConsentCategory.Analytics), true);
            _marketingToggle = CreateCheckbox(panel.transform, _marketingLabel, new Vector2(0, 5),
                ConsentStore.GetConsent(ConsentCategory.Marketing), true);
            _functionalToggle = CreateCheckbox(panel.transform, _functionalLabel, new Vector2(0, -30),
                ConsentStore.GetConsent(ConsentCategory.Functional), true);

            _agreeButton = CreateButton(panel.transform, _agreeLabel, new Vector2(-100, -100));
            _agreeButton.onClick.AddListener(() => OnResult(canvas, onResult));
            // Required true 조건부 활성 (기본 true 고정이지만 하위 호환 로직 포함).
            _agreeButton.interactable = _requiredToggle == null || _requiredToggle.isOn;

            var decline = CreateButton(panel.transform, _declineLabel, new Vector2(100, -100));
            decline.onClick.AddListener(() =>
            {
                if (_analyticsToggle != null) _analyticsToggle.isOn = false;
                if (_marketingToggle != null) _marketingToggle.isOn = false;
                if (_functionalToggle != null) _functionalToggle.isOn = false;
                OnResult(canvas, onResult);
            });

            if (_requiredToggle != null)
                _requiredToggle.onValueChanged.AddListener(on =>
                {
                    if (_agreeButton != null) _agreeButton.interactable = on;
                });
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("ConsentPanel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(640, 420);
            rt.anchoredPosition = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.9f);
            return panel;
        }

        private static Text CreateText(Transform parent, string content, int fontSize, Vector2 anchored)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520, 60);
            rt.anchoredPosition = anchored;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static Toggle CreateCheckbox(Transform parent, string label, Vector2 anchored, bool defaultOn, bool interactable)
        {
            var go = new GameObject("Toggle_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440, 30);
            rt.anchoredPosition = anchored;

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = defaultOn;
            toggle.interactable = interactable;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.sizeDelta = rt.sizeDelta;
            trt.anchoredPosition = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = (toggle.isOn ? "[x] " : "[ ] ") + label;
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = interactable ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            toggle.onValueChanged.AddListener(on =>
            {
                text.text = (on ? "[x] " : "[ ] ") + label;
            });

            return toggle;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 anchored)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 50);
            rt.anchoredPosition = anchored;
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.85f);
            var button = go.AddComponent<Button>();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.sizeDelta = rt.sizeDelta;
            trt.anchoredPosition = Vector2.zero;
            var text = textGo.AddComponent<Text>();
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            return button;
        }

        private void OnResult(Canvas canvas, Action<bool> onResult)
        {
            bool required = _requiredToggle == null || _requiredToggle.isOn;
            bool analytics = _analyticsToggle != null && _analyticsToggle.isOn;
            bool marketing = _marketingToggle != null && _marketingToggle.isOn;
            bool functional = _functionalToggle == null || _functionalToggle.isOn;

            ApplyConsent(required, analytics, marketing, functional);

            if (canvas != _canvas && canvas != null)
                Destroy(canvas.gameObject);

            onResult?.Invoke(analytics);
        }

        private void ApplyConsent(bool required, bool analytics, bool marketing, bool functional)
        {
            ConsentStore.SetConsent(ConsentCategory.Required, required);
            ConsentStore.SetConsent(ConsentCategory.Analytics, analytics);
            ConsentStore.SetConsent(ConsentCategory.Marketing, marketing);
            ConsentStore.SetConsent(ConsentCategory.Functional, functional);

            int version = _config != null ? _config.ConsentVersion : 1;
            ConsentStore.SetAcceptedVersion(version);

            if (_analytics != null)
                _analytics.IsEnabled = analytics;

#pragma warning disable CS0618 // Accepted 는 obsolete 지만 하위 호환용으로 채움
            _eventBus?.Publish(new BackendConsentChangedEvent
            {
                Required = required,
                Analytics = analytics,
                Marketing = marketing,
                Functional = functional,
                Accepted = analytics,
            });
#pragma warning restore CS0618

            Debug.Log($"[BackendConsentDialog] 동의 결과: required={required}, analytics={analytics}, marketing={marketing}, functional={functional}, version={version}");
        }
    }
}
