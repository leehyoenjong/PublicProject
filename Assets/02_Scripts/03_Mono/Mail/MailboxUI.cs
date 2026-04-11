using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 우편함 메인 화면. 필터, 전체 수령.
    /// </summary>
    public class MailboxUI : MonoBehaviour
    {
        [SerializeField] private Transform _mailListParent;
        [SerializeField] private GameObject _mailSlotPrefab;
        [SerializeField] private Button _claimAllButton;
        [SerializeField] private Button _deleteClaimedButton;
        [SerializeField] private Text _mailCountText;

        [Header("필터 버튼")]
        [SerializeField] private Button _filterAllButton;
        [SerializeField] private Button _filterSystemButton;
        [SerializeField] private Button _filterRewardButton;
        [SerializeField] private Button _filterEventButton;

        private IMailSystem _mailSystem;
        private IEventBus _eventBus;
        private MailType? _currentFilter;

        private void Start()
        {
            _mailSystem = ServiceLocator.Get<IMailSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<MailReceivedEvent>(OnMailReceived);
            _eventBus.Subscribe<MailClaimedEvent>(OnMailClaimed);
            _eventBus.Subscribe<MailClaimAllEvent>(OnClaimAll);
            _eventBus.Subscribe<MailDeletedEvent>(OnMailDeleted);

            if (_claimAllButton != null) _claimAllButton.onClick.AddListener(OnClaimAllClick);
            if (_deleteClaimedButton != null) _deleteClaimedButton.onClick.AddListener(OnDeleteClaimedClick);
            if (_filterAllButton != null) _filterAllButton.onClick.AddListener(() => SetFilter(null));
            if (_filterSystemButton != null) _filterSystemButton.onClick.AddListener(() => SetFilter(MailType.System));
            if (_filterRewardButton != null) _filterRewardButton.onClick.AddListener(() => SetFilter(MailType.Reward));
            if (_filterEventButton != null) _filterEventButton.onClick.AddListener(() => SetFilter(MailType.Event));

            RefreshList();
            Debug.Log("[MailboxUI] Init started");
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<MailReceivedEvent>(OnMailReceived);
            _eventBus.Unsubscribe<MailClaimedEvent>(OnMailClaimed);
            _eventBus.Unsubscribe<MailClaimAllEvent>(OnClaimAll);
            _eventBus.Unsubscribe<MailDeletedEvent>(OnMailDeleted);
        }

        public void RefreshList()
        {
            ClearList();

            IReadOnlyList<MailData> mails = _currentFilter.HasValue
                ? _mailSystem.GetMailsByType(_currentFilter.Value)
                : _mailSystem.GetAllMails();

            if (_mailListParent != null && _mailSlotPrefab != null)
            {
                foreach (MailData mail in mails)
                {
                    if (mail.State == MailState.Expired) continue;

                    GameObject slotObj = Instantiate(_mailSlotPrefab, _mailListParent);
                    Text titleText = slotObj.GetComponentInChildren<Text>();
                    if (titleText != null)
                    {
                        string prefix = mail.State == MailState.Unread ? "[NEW] " : "";
                        string reward = mail.HasRewards && mail.State != MailState.Claimed ? " [!]" : "";
                        titleText.text = $"{prefix}{mail.Title}{reward}";
                    }
                }
            }

            UpdateCountText();
        }

        private void SetFilter(MailType? filter)
        {
            _currentFilter = filter;
            RefreshList();
        }

        private void OnClaimAllClick()
        {
            _mailSystem.ClaimAll(_currentFilter);
        }

        private void OnDeleteClaimedClick()
        {
            _mailSystem.DeleteClaimedMails();
            RefreshList();
        }

        private void UpdateCountText()
        {
            if (_mailCountText == null) return;

            int unread = _mailSystem.GetUnreadCount();
            int claimable = _mailSystem.GetClaimableCount();
            _mailCountText.text = $"미읽음: {unread} | 수령 가능: {claimable}";
        }

        private void ClearList()
        {
            if (_mailListParent == null) return;

            for (int i = _mailListParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_mailListParent.GetChild(i).gameObject);
            }
        }

        private void OnMailReceived(MailReceivedEvent evt) { RefreshList(); }
        private void OnMailClaimed(MailClaimedEvent evt) { RefreshList(); }
        private void OnClaimAll(MailClaimAllEvent evt) { RefreshList(); }
        private void OnMailDeleted(MailDeletedEvent evt) { RefreshList(); }
    }
}
