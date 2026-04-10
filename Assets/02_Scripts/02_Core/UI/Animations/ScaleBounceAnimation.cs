using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 스케일 바운스 팝업 애니메이션.
    /// 출현 시 살짝 크게 나타났다가 원래 크기로 돌아오는 효과.
    /// </summary>
    public class ScaleBounceAnimation : IPopupAnimation
    {
        private readonly float _duration;
        private readonly float _overshoot;

        /// <param name="duration">애니메이션 시간</param>
        /// <param name="overshoot">바운스 오버슈트 크기 (1.0 = 정상, 1.2 = 20% 크게)</param>
        public ScaleBounceAnimation(float duration = 0.3f, float overshoot = 1.15f)
        {
            _duration = Mathf.Max(0.01f, duration);
            _overshoot = overshoot;
        }

        public IEnumerator PlayShow(RectTransform popup, CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 1f;
            popup.localScale = Vector3.zero;

            float elapsed = 0f;
            float bouncePoint = _duration * 0.6f;

            // Phase 1: 0 → overshoot (60% 시간)
            while (elapsed < bouncePoint)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / bouncePoint);
                float scale = Mathf.Lerp(0f, _overshoot, EaseOutQuad(t));
                popup.localScale = Vector3.one * scale;
                yield return null;
            }

            // Phase 2: overshoot → 1.0 (40% 시간)
            float remaining = _duration - bouncePoint;
            elapsed = 0f;
            while (elapsed < remaining)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / remaining);
                float scale = Mathf.Lerp(_overshoot, 1f, EaseInOutQuad(t));
                popup.localScale = Vector3.one * scale;
                yield return null;
            }

            popup.localScale = Vector3.one;
        }

        public IEnumerator PlayHide(RectTransform popup, CanvasGroup canvasGroup)
        {
            float elapsed = 0f;

            while (elapsed < _duration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / (_duration * 0.5f));
                float scale = Mathf.Lerp(1f, 0f, EaseInQuad(t));
                popup.localScale = Vector3.one * scale;
                canvasGroup.alpha = 1f - t;
                yield return null;
            }

            popup.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
        }

        private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseInQuad(float t) => t * t;
        private static float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
}
