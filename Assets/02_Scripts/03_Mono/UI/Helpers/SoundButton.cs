using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// Button 클릭 시 지정된 SFX를 자동 재생. Inspector에 SoundId만 넣으면 됨.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SoundButton : MonoBehaviour
    {
        [SerializeField] private string _clickSoundId = "UI_Click";

        private Button _button;

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
            if (string.IsNullOrEmpty(_clickSoundId)) return;

            ISoundManager sound = ServiceLocator.Get<ISoundManager>();
            sound?.PlaySFX(_clickSoundId);
        }
    }
}
