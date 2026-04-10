using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    public class AlertPopup : BasePopup
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;

        public override void Show(object data)
        {
            base.Show(data);

            if (data is AlertPopupData popupData)
            {
                _titleText.text = popupData.Title;
                _messageText.text = popupData.Message;
            }

            Debug.Log($"[AlertPopup] Shown.");
        }

        private void OnEnable()
        {
            _confirmButton.onClick.AddListener(OnConfirm);
        }

        private void OnDisable()
        {
            _confirmButton.onClick.RemoveListener(OnConfirm);
        }

        private void OnConfirm()
        {
            Debug.Log("[AlertPopup] Confirmed.");
            SetResult(PopupResult.Confirm);
        }
    }
}
