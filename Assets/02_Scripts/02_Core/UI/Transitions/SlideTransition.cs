using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 슬라이드 화면 전환.
    /// from이 왼쪽으로 나가고 to가 오른쪽에서 들어온다.
    /// </summary>
    public class SlideTransition : IScreenTransition
    {
        private readonly float _duration;
        private readonly Vector2 _direction;

        /// <param name="duration">전환 시간</param>
        /// <param name="direction">슬라이드 방향 (기본: 왼쪽으로)</param>
        public SlideTransition(float duration = 0.3f, Vector2? direction = null)
        {
            _duration = Mathf.Max(0.01f, duration);
            _direction = direction ?? Vector2.left;
        }

        public IEnumerator Execute(CanvasGroup from, CanvasGroup to)
        {
            var fromRect = from.GetComponent<RectTransform>();
            var toRect = to.GetComponent<RectTransform>();

            if (fromRect == null || toRect == null)
            {
                Debug.LogError("[SlideTransition] RectTransform not found.");
                from.alpha = 0f;
                to.alpha = 1f;
                yield break;
            }

            float screenWidth = fromRect.rect.width;
            Vector2 fromStart = Vector2.zero;
            Vector2 fromEnd = _direction * screenWidth;
            Vector2 toStart = -_direction * screenWidth;
            Vector2 toEnd = Vector2.zero;

            toRect.anchoredPosition = toStart;
            to.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < _duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / _duration);
                float smooth = t * t * (3f - 2f * t); // SmoothStep

                fromRect.anchoredPosition = Vector2.Lerp(fromStart, fromEnd, smooth);
                toRect.anchoredPosition = Vector2.Lerp(toStart, toEnd, smooth);
                yield return null;
            }

            fromRect.anchoredPosition = fromEnd;
            toRect.anchoredPosition = toEnd;
            from.alpha = 0f;

            // 위치 복원 (다음 사용을 위해)
            fromRect.anchoredPosition = Vector2.zero;
        }
    }
}
