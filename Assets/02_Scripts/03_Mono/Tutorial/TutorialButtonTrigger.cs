using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// Button 클릭 시 튜토리얼 트리거 발동. _triggerValue가 비어있으면 GameObject 이름 사용.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class TutorialButtonTrigger : MonoBehaviour
    {
        [SerializeField] private string _triggerValue;
        [SerializeField] private bool _onlyOnce;

        private Button _button;
        private bool _fired;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        private void OnClick()
        {
            if (_onlyOnce && _fired) return;

            ITutorialSystem tutorial = ServiceLocator.Get<ITutorialSystem>();
            if (tutorial == null) return;

            string value = string.IsNullOrEmpty(_triggerValue) ? name : _triggerValue;
            tutorial.CheckTriggers(TriggerType.ButtonClick, value);
            _fired = true;
        }
    }
}
