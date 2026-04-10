using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 페이드 인/아웃 화면 전환.
    /// from을 페이드 아웃한 후 to를 페이드 인한다.
    /// </summary>
    public class FadeTransition : IScreenTransition
    {
        private readonly float _duration;

        public FadeTransition(float duration = 0.3f)
        {
            _duration = Mathf.Max(0.01f, duration);
        }

        public IEnumerator Execute(CanvasGroup from, CanvasGroup to)
        {
            float elapsed = 0f;

            // Fade out
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                from.alpha = 1f - t;
                yield return null;
            }
            from.alpha = 0f;

            // Fade in
            elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                to.alpha = t;
                yield return null;
            }
            to.alpha = 1f;
        }
    }
}
