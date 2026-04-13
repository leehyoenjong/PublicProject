using System;
using System.Collections;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// CanvasGroup 페이드 인/아웃 헬퍼. interactable/blocksRaycasts 연동.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupFader : MonoBehaviour
    {
        [SerializeField] private float _defaultDuration = 0.3f;

        private CanvasGroup _group;
        private Coroutine _routine;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
        }

        public void FadeIn(float duration = -1f, Action onComplete = null)
        {
            StartFade(1f, duration, true, onComplete);
        }

        public void FadeOut(float duration = -1f, Action onComplete = null)
        {
            StartFade(0f, duration, false, onComplete);
        }

        public void SetVisible(bool visible)
        {
            if (_routine != null) StopCoroutine(_routine);
            _group.alpha = visible ? 1f : 0f;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
        }

        private void StartFade(float target, float duration, bool interactable, Action onComplete)
        {
            if (duration < 0f) duration = _defaultDuration;
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FadeRoutine(target, duration, interactable, onComplete));
        }

        private IEnumerator FadeRoutine(float target, float duration, bool interactable, Action onComplete)
        {
            float start = _group.alpha;
            float t = 0f;

            _group.interactable = interactable;
            _group.blocksRaycasts = interactable;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }

            _group.alpha = target;
            _routine = null;
            onComplete?.Invoke();
        }
    }
}
