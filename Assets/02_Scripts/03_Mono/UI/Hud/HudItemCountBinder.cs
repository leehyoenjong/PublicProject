using System;
using TMPro;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// IInventorySystem 의 임의 MID 보유량을 TMP 텍스트로 표시.
    /// MID 와 표시 포맷을 인스펙터에서 결정하므로 골드/다이아/포션/재료 등 모든 stack/instance 아이템에 재사용.
    /// EventBus 의 ItemAcquired/Consumed/Converted/Expired 4종을 구독해 즉시 갱신.
    /// 디버그 ContextMenu 로 +100 / -50 트리거 가능 (UNITY_EDITOR).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class HudItemCountBinder : MonoBehaviour
    {
        [Header("표시 대상 (ItemData MID — 인스펙터 필수 입력)")]
        [SerializeField] private int _itemMid;

        [Header("표시 포맷 ({0} = 보유량)")]
        [SerializeField] private string _format = "{0:N0}";

        private TMP_Text _text;
        private IInventorySystem _inventory;
        private IEventBus _eventBus;
        private Action<ItemAcquiredEvent> _onAcquired;
        private Action<ItemConsumedEvent> _onConsumed;
        private Action<ItemConvertedEvent> _onConverted;
        private Action<ItemExpiredEvent> _onExpired;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            _inventory = ServiceLocator.Has<IInventorySystem>() ? ServiceLocator.Get<IInventorySystem>() : null;
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;

            if (_eventBus != null)
            {
                _onAcquired = _ => Refresh();
                _onConsumed = _ => Refresh();
                _onConverted = _ => Refresh();
                _onExpired = _ => Refresh();
                _eventBus.Subscribe(_onAcquired);
                _eventBus.Subscribe(_onConsumed);
                _eventBus.Subscribe(_onConverted);
                _eventBus.Subscribe(_onExpired);
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (_eventBus == null) return;
            if (_onAcquired != null) _eventBus.Unsubscribe(_onAcquired);
            if (_onConsumed != null) _eventBus.Unsubscribe(_onConsumed);
            if (_onConverted != null) _eventBus.Unsubscribe(_onConverted);
            if (_onExpired != null) _eventBus.Unsubscribe(_onExpired);
        }

        public void Refresh()
        {
            if (_text == null) return;
            int count = _inventory != null ? _inventory.GetCount(_itemMid) : 0;
            int max = _inventory != null ? _inventory.GetMaxStack(_itemMid) : 0;
            _text.text = string.Format(_format, count, max);
        }

#if UNITY_EDITOR
        [ContextMenu("디버그/+100")]
        private void DebugAdd100()
        {
            if (_inventory == null)
            {
                Debug.LogWarning("[HUD] IInventorySystem 미등록 — 디버그 +100 무시");
                return;
            }
            _inventory.AddItem(_itemMid, 100, "debug");
        }

        [ContextMenu("디버그/-50")]
        private void DebugConsume50()
        {
            if (_inventory == null)
            {
                Debug.LogWarning("[HUD] IInventorySystem 미등록 — 디버그 -50 무시");
                return;
            }
            _inventory.ConsumeByMID(_itemMid, 50);
        }
#endif
    }
}
