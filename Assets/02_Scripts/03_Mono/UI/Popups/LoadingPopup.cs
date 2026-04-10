using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LoadingPopup : MonoBehaviour
    {
        [SerializeField] private Text _messageText;
        [SerializeField] private Image _progressBar;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public void Setup(string message = "Loading...")
        {
            _messageText.text = message;
            UpdateProgress(0f);
            Debug.Log($"[LoadingPopup] Setup: {message}");
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            Debug.Log("[LoadingPopup] Shown. Input blocked.");
        }

        public void Hide()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            Debug.Log("[LoadingPopup] Hidden. Input unblocked.");
        }

        public void UpdateProgress(float ratio)
        {
            if (_progressBar != null)
            {
                _progressBar.fillAmount = Mathf.Clamp01(ratio);
            }
        }

        public void SetMessage(string message)
        {
            _messageText.text = message;
        }
    }
}
