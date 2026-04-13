using System;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// GDPR 동의 다이얼로그. 프리팹 바이너리를 만들지 않고 런타임에 UGUI 를 즉시 조립한다.
    /// 프로젝트에서 커스텀 Canvas 를 <see cref="_canvas"/> 에 주입하면 그 Canvas 하위로 UI 를 생성하고,
    /// 주입이 없으면 기본 Canvas 를 새로 만들어 사용한다.
    /// 결과는 <see cref="Show"/> 의 onResult 콜백과 <see cref="BackendConsentChangedEvent"/> 로 동시 전달.
    /// </summary>
    public class BackendConsentDialog : MonoBehaviour
    {
        private const string PREF_ACCEPTED_VERSION = "consent.acceptedVersion";
        private const string CANVAS_NAME = "ConsentCanvas";

        [SerializeField] private Canvas _canvas;
        [SerializeField] private string _titleText = "Consent";
        [SerializeField] private string _bodyText = "Do you agree to anonymous analytics and crash reports?";
        [SerializeField] private string _agreeLabel = "Agree";
        [SerializeField] private string _declineLabel = "Decline";

        private BackendConfig _config;
        private IBackendAnalytics _analytics;
        private IEventBus _eventBus;

        /// <summary>
        /// 저장된 동의 버전이 현재 Config.ConsentVersion 과 다르면 재동의 필요 (true).
        /// </summary>
        public static bool RequiresConsent(BackendConfig config)
        {
            if (config == null) return false;
            int accepted = PlayerPrefs.GetInt(PREF_ACCEPTED_VERSION, 0);
            return accepted != config.ConsentVersion;
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
            CreateText(panel.transform, _titleText, 24, new Vector2(0, 80));
            CreateText(panel.transform, _bodyText, 16, new Vector2(0, 10));

            var agree = CreateButton(panel.transform, _agreeLabel, new Vector2(-100, -80));
            agree.onClick.AddListener(() => OnResult(canvas, true, onResult));

            var decline = CreateButton(panel.transform, _declineLabel, new Vector2(100, -80));
            decline.onClick.AddListener(() => OnResult(canvas, false, onResult));
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("ConsentPanel");
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(520, 260);
            rt.anchoredPosition = Vector2.zero;
            var image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.85f);
            return panel;
        }

        private static Text CreateText(Transform parent, string content, int fontSize, Vector2 anchored)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(480, 80);
            rt.anchoredPosition = anchored;
            var text = go.AddComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
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

        private void OnResult(Canvas canvas, bool accepted, Action<bool> onResult)
        {
            ApplyConsent(accepted);
            onResult?.Invoke(accepted);

            if (canvas != _canvas && canvas != null)
                Destroy(canvas.gameObject);
        }

        private void ApplyConsent(bool accepted)
        {
            int version = _config != null ? _config.ConsentVersion : 1;
            PlayerPrefs.SetInt(PREF_ACCEPTED_VERSION, accepted ? version : 0);
            PlayerPrefs.Save();

            if (_analytics != null)
                _analytics.IsEnabled = accepted;

            _eventBus?.Publish(new BackendConsentChangedEvent { Accepted = accepted });
            Debug.Log($"[BackendConsentDialog] 동의 결과: accepted={accepted}, version={version}");
        }
    }
}
