using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 우편 상세 팝업. 내용 표시 + 보상 수령.
    /// </summary>
    public class MailDetailPopup : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _senderText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Text _dateText;
        [SerializeField] private Transform _rewardParent;
        [SerializeField] private GameObject _rewardSlotPrefab;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _deleteButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanvasGroup _canvasGroup;

        private IMailSystem _mailSystem;
        private string _currentMailId;

        private void Start()
        {
            _mailSystem = ServiceLocator.Get<IMailSystem>();

            if (_claimButton != null) _claimButton.onClick.AddListener(OnClaim);
            if (_deleteButton != null) _deleteButton.onClick.AddListener(OnDelete);
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);

            SetVisible(false);
        }

        public void Show(string mailId)
        {
            _currentMailId = mailId;
            MailData mail = _mailSystem.GetMail(mailId);
            if (mail == null) return;

            _mailSystem.ReadMail(mailId);

            if (_titleText != null) _titleText.text = mail.Title;
            if (_senderText != null) _senderText.text = mail.SenderName;
            if (_bodyText != null) _bodyText.text = mail.Body;
            if (_dateText != null) _dateText.text = mail.SentTime;

            ClearRewards();

            if (mail.HasRewards && _rewardParent != null && _rewardSlotPrefab != null)
            {
                foreach (MailRewardEntry reward in mail.Rewards)
                {
                    GameObject slotObj = Instantiate(_rewardSlotPrefab, _rewardParent);
                    Text rewardText = slotObj.GetComponentInChildren<Text>();
                    if (rewardText != null)
                    {
                        rewardText.text = $"{reward.RewardId} x{reward.Amount}";
                    }
                }
            }

            bool canClaim = mail.HasRewards && mail.State != MailState.Claimed;
            if (_claimButton != null) _claimButton.interactable = canClaim;

            SetVisible(true);
            Debug.Log($"[MailDetailPopup] Show: {mailId}");
        }

        public void Hide()
        {
            _currentMailId = null;
            SetVisible(false);
        }

        private void OnClaim()
        {
            if (string.IsNullOrEmpty(_currentMailId)) return;

            bool claimed = _mailSystem.ClaimMail(_currentMailId);

            if (claimed && _claimButton != null)
            {
                _claimButton.interactable = false;
            }
        }

        private void OnDelete()
        {
            if (string.IsNullOrEmpty(_currentMailId)) return;

            _mailSystem.DeleteMail(_currentMailId);
            Hide();
        }

        private void ClearRewards()
        {
            if (_rewardParent == null) return;

            for (int i = _rewardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_rewardParent.GetChild(i).gameObject);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.interactable = visible;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}
