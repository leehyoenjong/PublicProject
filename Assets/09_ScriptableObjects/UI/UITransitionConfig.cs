using UnityEngine;

namespace PublicFramework
{
    [CreateAssetMenu(fileName = "UITransitionConfig", menuName = "PublicFramework/UI/TransitionConfig")]
    public class UITransitionConfig : ScriptableObject
    {
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _scaleDuration = 0.2f;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _toastDisplayDuration = 2f;
        [SerializeField] private float _toastFadeDuration = 0.5f;

        public float FadeDuration => _fadeDuration;
        public float ScaleDuration => _scaleDuration;
        public AnimationCurve FadeCurve => _fadeCurve;
        public AnimationCurve ScaleCurve => _scaleCurve;
        public float ToastDisplayDuration => _toastDisplayDuration;
        public float ToastFadeDuration => _toastFadeDuration;
    }
}
