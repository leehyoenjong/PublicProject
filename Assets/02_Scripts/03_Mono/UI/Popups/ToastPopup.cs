using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    public class ToastPopup : BaseOverlay
    {
        [SerializeField] private Text _messageText;
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _fadeDuration = 0.5f;

        public void Setup(string message, UITransitionConfig config = null)
        {
            _messageText.text = message;

            if (config != null)
            {
                _displayDuration = config.ToastDisplayDuration;
                _fadeDuration = config.ToastFadeDuration;
            }

            Debug.Log($"[ToastPopup] Setup: {message}");
        }

        public void ShowAndAutoHide()
        {
            Show();
            StartCoroutine(AutoHideSequence());
        }

        private IEnumerator AutoHideSequence()
        {
            yield return new WaitForSecondsRealtime(_displayDuration);

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                CanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
                yield return null;
            }

            Debug.Log("[ToastPopup] Auto-hidden.");
            Destroy(gameObject);
        }
    }
}
