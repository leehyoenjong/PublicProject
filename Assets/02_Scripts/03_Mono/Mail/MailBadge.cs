using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 미수령 우편 배지 위젯. 미수령 수 표시 + 자동 갱신.
    /// </summary>
    public class MailBadge : MonoBehaviour
    {
        private const int BADGE_MAX_DISPLAY = 99;

        [SerializeField] private Text _countText;
        [SerializeField] private GameObject _badgeRoot;

        private IMailSystem _mailSystem;
        private IEventBus _eventBus;

        private void Start()
        {
            _mailSystem = ServiceLocator.Get<IMailSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<MailReceivedEvent>(OnMailChanged);
            _eventBus.Subscribe<MailClaimedEvent>(OnClaimed);
            _eventBus.Subscribe<MailClaimAllEvent>(OnClaimAll);
            _eventBus.Subscribe<MailReadEvent>(OnRead);

            UpdateBadge();
            Debug.Log("[MailBadge] Init started");
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<MailReceivedEvent>(OnMailChanged);
            _eventBus.Unsubscribe<MailClaimedEvent>(OnClaimed);
            _eventBus.Unsubscribe<MailClaimAllEvent>(OnClaimAll);
            _eventBus.Unsubscribe<MailReadEvent>(OnRead);
        }

        private void UpdateBadge()
        {
            int claimable = _mailSystem.GetClaimableCount();

            if (_badgeRoot != null)
            {
                _badgeRoot.SetActive(claimable > 0);
            }

            if (_countText != null)
            {
                _countText.text = claimable > BADGE_MAX_DISPLAY ? $"{BADGE_MAX_DISPLAY}+" : claimable.ToString();
            }
        }

        private void OnMailChanged(MailReceivedEvent evt) { UpdateBadge(); }
        private void OnClaimed(MailClaimedEvent evt) { UpdateBadge(); }
        private void OnClaimAll(MailClaimAllEvent evt) { UpdateBadge(); }
        private void OnRead(MailReadEvent evt) { UpdateBadge(); }
    }
}
