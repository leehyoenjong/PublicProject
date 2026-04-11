using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 결과 화면 — 등급별 연출
    /// </summary>
    public class GachaResultUI : MonoBehaviour
    {
        [SerializeField] private Transform _rewardSlotParent;
        [SerializeField] private GameObject _rewardSlotPrefab;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _retryButton;

        [Header("등급별 색상")]
        [SerializeField] private Color _commonColor = Color.gray;
        [SerializeField] private Color _uncommonColor = Color.green;
        [SerializeField] private Color _rareColor = Color.blue;
        [SerializeField] private Color _epicColor = new Color(0.6f, 0f, 0.8f);
        [SerializeField] private Color _legendaryColor = Color.yellow;

        private IEventBus _eventBus;
        private string _lastBannerId;

        private void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _eventBus.Subscribe<GachaPullResultEvent>(OnPullResult);

            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_retryButton != null) _retryButton.onClick.AddListener(OnRetry);

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<GachaPullResultEvent>(OnPullResult);
        }

        private void OnPullResult(GachaPullResultEvent evt)
        {
            _lastBannerId = evt.BannerId;
            ShowResults(evt.Rewards);
        }

        private void ShowResults(GachaReward[] rewards)
        {
            ClearSlots();

            if (_rewardSlotParent == null || _rewardSlotPrefab == null) return;

            foreach (GachaReward reward in rewards)
            {
                GameObject slotObj = Instantiate(_rewardSlotPrefab, _rewardSlotParent);

                Text nameText = slotObj.GetComponentInChildren<Text>();
                if (nameText != null)
                {
                    nameText.text = $"{reward.RewardId} ({reward.Grade})";
                    nameText.color = GetGradeColor(reward.Grade);
                }

                Image bgImage = slotObj.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = GetGradeColor(reward.Grade) * 0.3f + Color.white * 0.7f;
                }
            }

            SetVisible(true);
            Debug.Log($"[GachaResultUI] Showing {rewards.Length} rewards");
        }

        private void Hide()
        {
            SetVisible(false);
        }

        private void OnRetry()
        {
            Hide();

            if (!string.IsNullOrEmpty(_lastBannerId))
            {
                IGachaSystem gachaSystem = ServiceLocator.Get<IGachaSystem>();
                gachaSystem.Pull(_lastBannerId, 1);
            }
        }

        private void ClearSlots()
        {
            if (_rewardSlotParent == null) return;

            for (int i = _rewardSlotParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_rewardSlotParent.GetChild(i).gameObject);
            }
        }

        private Color GetGradeColor(ItemGrade grade)
        {
            return grade switch
            {
                ItemGrade.Common => _commonColor,
                ItemGrade.Uncommon => _uncommonColor,
                ItemGrade.Rare => _rareColor,
                ItemGrade.Epic => _epicColor,
                ItemGrade.Legendary => _legendaryColor,
                _ => Color.white
            };
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
