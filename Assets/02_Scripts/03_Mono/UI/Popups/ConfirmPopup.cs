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

            Debug.Log("[확인팝업] 표시됨.");
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
            Debug.Log("[확인팝업] 확인됨.");
            SetResult(PopupResult.Confirm);
        }

        private void OnCancel()
        {
            Debug.Log("[확인팝업] 취소됨.");
            SetResult(PopupResult.Cancel);
        }
    }
}
