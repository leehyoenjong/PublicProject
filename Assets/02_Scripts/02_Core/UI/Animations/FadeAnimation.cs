using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 페이드 인/아웃 팝업 애니메이션.
    /// </summary>
    public class FadeAnimation : IPopupAnimation
    {
        private readonly float _duration;

        public FadeAnimation(float duration = 0.2f)
        {
            _duration = Mathf.Max(0.01f, duration);
        }

        public IEnumerator PlayShow(RectTransform popup, CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / _duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        public IEnumerator PlayHide(RectTransform popup, CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 1f;
            float elapsed = 0f;

            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / _duration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }
    }
}
