using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// UI(Screen/Panel)가 활성화되는 시점에 튜토리얼 트리거 발동.
    /// 같은 GameObject에 BaseScreen이 있으면 ScreenId를 자동 사용.
    /// 없으면 _triggerValue를 사용.
    /// </summary>
    public class TutorialUITrigger : MonoBehaviour
    {
        [SerializeField] private string _triggerValue;
        [SerializeField] private bool _onlyOnce = true;

        private bool _fired;

        private void OnEnable()
        {
            if (_onlyOnce && _fired) return;

            ITutorialSystem tutorial = ServiceLocator.Get<ITutorialSystem>();
            if (tutorial == null) return;

            string value = ResolveTriggerValue();
            tutorial.CheckTriggers(TriggerType.UIOpen, value);
            _fired = true;
        }

        private string ResolveTriggerValue()
        {
            if (!string.IsNullOrEmpty(_triggerValue)) return _triggerValue;

            BaseScreen screen = GetComponent<BaseScreen>();
            if (screen != null) return screen.ScreenId;

            return name;
        }
    }
}
