using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    public class ConfirmPopup : BasePopup
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        public override void Show(object data)
        {
            base.Show(data);

            if (data is ConfirmPopupData popupData)
            {
                _titleText.text = popupData.Title;
                _messageText.text = popupData.Message;
            }

            Debug.Log($"[ConfirmPopup] Shown.");
        }

        private void OnEnable()
        {
            _confirmButton.onClick.AddListener(OnConfirm);
            _cancelButton.onClick.AddListener(OnCancel);
        }

        private void OnDisable()
        {
            _confirmButton.onClick.RemoveListener(OnConfirm);
            _cancelButton.onClick.RemoveListener(OnCancel);
        }

        private void OnConfirm()
        {
            Debug.Log("[ConfirmPopup] Confirmed.");
            SetResult(PopupResult.Confirm);
        }

        private void OnCancel()
        {
            Debug.Log("[ConfirmPopup] Cancelled.");
            SetResult(PopupResult.Cancel);
        }
    }
}
