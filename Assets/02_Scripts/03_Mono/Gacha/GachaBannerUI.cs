using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 배너 선택/표시 UI
    /// </summary>
    public class GachaBannerUI : MonoBehaviour
    {
        private const int DEFAULT_MULTI_COUNT = 10;

        [SerializeField] private Image _bannerImage;
        [SerializeField] private Text _bannerNameText;
        [SerializeField] private Text _bannerDescText;
        [SerializeField] private Text _singleCostText;
        [SerializeField] private Text _multiCostText;
        [SerializeField] private Text _pityCountText;
        [SerializeField] private Button _singlePullButton;
        [SerializeField] private Button _multiPullButton;
        [SerializeField] private Transform _bannerListParent;

        private IGachaSystem _gachaSystem;
        private IEventBus _eventBus;
        private string _selectedBannerId;

        private void Start()
        {
            _gachaSystem = ServiceLocator.Get<IGachaSystem>();
            _eventBus = ServiceLocator.Get<IEventBus>();

            _eventBus.Subscribe<GachaPullResultEvent>(OnPullResult);
            _eventBus.Subscribe<GachaPityReachedEvent>(OnPityReached);

            if (_singlePullButton != null) _singlePullButton.onClick.AddListener(OnSinglePull);
            if (_multiPullButton != null) _multiPullButton.onClick.AddListener(OnMultiPull);

            RefreshBannerList();
            Debug.Log("[GachaBannerUI] Init started");
        }

        private void OnDestroy()
        {
            if (_eventBus == null) return;

            _eventBus.Unsubscribe<GachaPullResultEvent>(OnPullResult);
            _eventBus.Unsubscribe<GachaPityReachedEvent>(OnPityReached);
        }

        public void SelectBanner(string bannerId)
        {
            _selectedBannerId = bannerId;
            GachaBannerData banner = _gachaSystem.GetBannerInfo(bannerId);
            if (banner == null) return;

            if (_bannerImage != null && banner.BannerImage != null) _bannerImage.sprite = banner.BannerImage;
            if (_bannerNameText != null) _bannerNameText.text = banner.DisplayName;
            if (_bannerDescText != null) _bannerDescText.text = banner.Description;
            if (_singleCostText != null) _singleCostText.text = banner.PullCostSingle.ToString();
            if (_multiCostText != null) _multiCostText.text = banner.PullCostMulti.ToString();

            UpdatePityDisplay();
        }

        private void RefreshBannerList()
        {
            IReadOnlyList<GachaBannerData> banners = _gachaSystem.GetActiveBanners();

            if (banners.Count > 0)
            {
                SelectBanner(banners[0].BannerId);
            }
        }

        private void UpdatePityDisplay()
        {
            if (_pityCountText == null || string.IsNullOrEmpty(_selectedBannerId)) return;

            PityCounter pity = _gachaSystem.GetPityInfo(_selectedBannerId);
            GachaBannerData banner = _gachaSystem.GetBannerInfo(_selectedBannerId);

            if (banner != null && banner.HardPityCount > 0)
            {
                _pityCountText.text = $"{pity.PullCount}/{banner.HardPityCount}";
            }
            else
            {
                _pityCountText.text = pity.PullCount.ToString();
            }
        }

        private void OnSinglePull()
        {
            if (string.IsNullOrEmpty(_selectedBannerId)) return;
            _gachaSystem.Pull(_selectedBannerId, 1);
        }

        private void OnMultiPull()
        {
            if (string.IsNullOrEmpty(_selectedBannerId)) return;

            GachaBannerData banner = _gachaSystem.GetBannerInfo(_selectedBannerId);
            int count = banner != null ? banner.MultiPullCount : DEFAULT_MULTI_COUNT;
            _gachaSystem.Pull(_selectedBannerId, count);
        }

        private void OnPullResult(GachaPullResultEvent evt)
        {
            if (evt.BannerId != _selectedBannerId) return;
            UpdatePityDisplay();
        }

        private void OnPityReached(GachaPityReachedEvent evt)
        {
            if (evt.BannerId != _selectedBannerId) return;
            Debug.Log($"[GachaBannerUI] Pity reached! ({evt.PityType})");
        }
    }
}
