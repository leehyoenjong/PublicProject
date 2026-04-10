using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PublicFramework
{
    /// <summary>
    /// Priority Queue 기반 팝업 매니저.
    /// 높은 우선순위 팝업이 위에 표시되며, 같은 우선순위는 LIFO.
    /// </summary>
    public class PopupManager : IPopupManager
    {
        private const int SYSTEM_ALERT_PRIORITY = 100;

        private readonly List<PopupEntry> _popupQueue = new();
        private readonly Dictionary<string, BasePopup> _popupPrefabs = new();
        private readonly Dictionary<string, BasePopup> _popupInstances = new();
        private readonly Transform _popupRoot;
        private readonly GameObject _dimBackground;
        private readonly MonoBehaviour _coroutineRunner;

        private BasePopup _currentPopup;
        private IPopupAnimation _defaultAnimation;

        public int PopupCount => _popupQueue.Count + (_currentPopup != null ? 1 : 0);

        public event Action<BasePopup> OnPopupOpened;
        public event Action<BasePopup> OnPopupClosed;

        public PopupManager(Transform popupRoot, GameObject dimBackground, MonoBehaviour coroutineRunner)
        {
            _popupRoot = popupRoot ?? throw new ArgumentNullException(nameof(popupRoot));
            _coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
            _dimBackground = dimBackground;
            Debug.Log("[PopupManager] Init completed.");
        }

        public void SetDefaultAnimation(IPopupAnimation animation)
        {
            _defaultAnimation = animation;
        }

        public void RegisterPopup(string popupId, BasePopup prefab)
        {
            if (_popupPrefabs.ContainsKey(popupId))
            {
                Debug.LogWarning($"[PopupManager] Popup '{popupId}' already registered. Overwriting.");
            }
            _popupPrefabs[popupId] = prefab;
        }

        public void Show(string popupId, object data = null, int priority = 0)
        {
            if (priority >= SYSTEM_ALERT_PRIORITY)
            {
                ShowImmediate(popupId, data, priority);
                return;
            }

            if (_currentPopup == null)
            {
                ShowImmediate(popupId, data, priority);
            }
            else
            {
                EnqueuePopup(popupId, data, priority);
            }
        }

        public void Hide()
        {
            if (_currentPopup == null) return;

            var closingPopup = _currentPopup;

            if (_defaultAnimation != null)
            {
                _coroutineRunner.StartCoroutine(HideWithAnimation(closingPopup));
            }
            else
            {
                FinishHide(closingPopup);
            }
        }

        public void HideAll()
        {
            if (_currentPopup != null)
            {
                _currentPopup.Hide();
                OnPopupClosed?.Invoke(_currentPopup);
                _currentPopup = null;
            }

            _popupQueue.Clear();
            UpdateDimBackground(false);
            Debug.Log("[PopupManager] All popups hidden.");
        }

        public BasePopup GetCurrentPopup()
        {
            return _currentPopup;
        }

        private void ShowImmediate(string popupId, object data, int priority)
        {
            var popup = GetOrCreatePopup(popupId);
            if (popup == null) return;

            if (_currentPopup != null)
            {
                _currentPopup.Hide();
                EnqueuePopup(_currentPopup.PopupId, _currentPopup.LastData, _currentPopup.Priority);
            }

            _currentPopup = popup;
            popup.transform.SetAsLastSibling();

            if (popup.IsModal)
                UpdateDimBackground(true);

            if (_defaultAnimation != null)
            {
                _coroutineRunner.StartCoroutine(ShowWithAnimation(popup, data));
            }
            else
            {
                popup.Show(data);
                OnPopupOpened?.Invoke(popup);
            }
        }

        private void EnqueuePopup(string popupId, object data, int priority)
        {
            var entry = new PopupEntry
            {
                PopupId = popupId,
                Data = data,
                Priority = priority,
                Timestamp = Time.unscaledTime
            };

            int insertIndex = 0;
            for (int i = 0; i < _popupQueue.Count; i++)
            {
                if (_popupQueue[i].Priority > priority)
                {
                    insertIndex = i + 1;
                }
                else if (_popupQueue[i].Priority == priority)
                {
                    insertIndex = i;
                    break;
                }
                else
                {
                    break;
                }
            }

            _popupQueue.Insert(insertIndex, entry);
            Debug.Log($"[PopupManager] Popup '{popupId}' queued at position {insertIndex}. Queue size: {_popupQueue.Count}");
        }

        private void ShowNextInQueue()
        {
            if (_popupQueue.Count == 0)
            {
                UpdateDimBackground(false);
                return;
            }

            var next = _popupQueue[0];
            _popupQueue.RemoveAt(0);
            ShowImmediate(next.PopupId, next.Data, next.Priority);
        }

        private void FinishHide(BasePopup popup)
        {
            popup.Hide();
            _currentPopup = null;
            OnPopupClosed?.Invoke(popup);
            ShowNextInQueue();
        }

        private BasePopup GetOrCreatePopup(string popupId)
        {
            if (_popupInstances.TryGetValue(popupId, out var existing))
                return existing;

            if (!_popupPrefabs.TryGetValue(popupId, out var prefab))
            {
                Debug.LogError($"[PopupManager] Popup '{popupId}' not registered.");
                return null;
            }

            var instance = Object.Instantiate(prefab, _popupRoot);
            instance.gameObject.SetActive(false);
            _popupInstances[popupId] = instance;
            return instance;
        }

        private void UpdateDimBackground(bool show)
        {
            if (_dimBackground != null)
            {
                _dimBackground.SetActive(show);
                if (show)
                    _dimBackground.transform.SetAsLastSibling();
            }
        }

        private IEnumerator ShowWithAnimation(BasePopup popup, object data)
        {
            popup.gameObject.SetActive(true);
            popup.CanvasGroup.alpha = 0f;
            yield return _defaultAnimation.PlayShow(popup.RectTransform, popup.CanvasGroup);
            popup.Show(data);
            OnPopupOpened?.Invoke(popup);
        }

        private IEnumerator HideWithAnimation(BasePopup popup)
        {
            yield return _defaultAnimation.PlayHide(popup.RectTransform, popup.CanvasGroup);
            FinishHide(popup);
        }

        private struct PopupEntry
        {
            public string PopupId;
            public object Data;
            public int Priority;
            public float Timestamp;
        }
    }
}
