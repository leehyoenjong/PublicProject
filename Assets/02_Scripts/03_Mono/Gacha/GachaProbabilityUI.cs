using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 가챠 확률 표시 화면
    /// </summary>
    public class GachaProbabilityUI : MonoBehaviour
    {
        [SerializeField] private Transform _entryParent;
        [SerializeField] private GameObject _entryPrefab;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Text _titleText;

        private void Start()
        {
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            SetVisible(false);
        }

        public void Show(string bannerId)
        {
            IGachaSystem gachaSystem = ServiceLocator.Get<IGachaSystem>();
            GachaBannerData banner = gachaSystem.GetBannerInfo(bannerId);

            if (banner == null) return;

            if (_titleText != null)
            {
                _titleText.text = $"{banner.DisplayName} - 확률 정보";
            }

            ClearEntries();
            IReadOnlyList<DropEntry> entries = gachaSystem.GetProbabilities(bannerId);

            if (_entryParent == null || _entryPrefab == null)
            {
                SetVisible(true);
                return;
            }

            // 등급별 확률 집계 (enum 순서로 정렬)
            SortedDictionary<ItemGrade, float> gradeWeights = new SortedDictionary<ItemGrade, float>();
            int totalWeight = 0;

            foreach (DropEntry entry in entries)
            {
                totalWeight += entry.Weight;

                if (!gradeWeights.ContainsKey(entry.Grade))
                {
                    gradeWeights[entry.Grade] = 0f;
                }
                gradeWeights[entry.Grade] += entry.Weight;
            }

            // 등급별 확률 표시 (Common → Legendary 순서)
            foreach (var kvp in gradeWeights)
            {
                float probability = totalWeight > 0 ? kvp.Value / totalWeight * 100f : 0f;
                CreateEntry($"{kvp.Key}", $"{probability:F2}%");
            }

            // 개별 아이템 표시
            foreach (DropEntry entry in entries)
            {
                float probability = totalWeight > 0 ? (float)entry.Weight / totalWeight * 100f : 0f;
                string label = entry.IsPickup ? $"[PICKUP] {entry.ItemId}" : entry.ItemId;
                CreateEntry(label, $"{probability:F3}%");
            }

            SetVisible(true);
            Debug.Log($"[GachaProbabilityUI] Showing probabilities for: {bannerId}");
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void CreateEntry(string label, string value)
        {
            if (_entryParent == null || _entryPrefab == null) return;

            GameObject entryObj = Instantiate(_entryPrefab, _entryParent);
            Text[] texts = entryObj.GetComponentsInChildren<Text>();

            if (texts.Length >= 2)
            {
                texts[0].text = label;
                texts[1].text = value;
            }
            else if (texts.Length == 1)
            {
                texts[0].text = $"{label}: {value}";
            }
        }

        private void ClearEntries()
        {
            if (_entryParent == null) return;

            for (int i = _entryParent.childCount - 1; i >= 0; i--)
            {
                Destroy(_entryParent.GetChild(i).gameObject);
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
