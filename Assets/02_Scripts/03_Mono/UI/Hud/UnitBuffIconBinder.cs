using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// UnitController 1체에 부착되어 적용된 버프를 World-Space 아이콘 패널로 시각화한다.
    /// BuffApplied/Removed/Expired/Refreshed/StackChanged 5종 이벤트를 구독하고,
    /// (BuffId+SourceSkillId) 페어를 키로 ui_buff_icon prefab 인스턴스를 추가/제거/갱신한다.
    /// 아이콘 스프라이트는 SourceSkillId 로 ISkillSystem.GetSkillData 를 조회해 SkillData.Icon 을 재활용한다.
    /// Duration overlay 는 Update 에서 IBuffSystem.GetBuffs 의 RemainingRatio 를 polling.
    /// </summary>
    public class UnitBuffIconBinder : MonoBehaviour
    {
        [Header("타겟 (비우면 GetComponentInParent 자동)")]
        [SerializeField] private UnitController _target;

        [Header("아이콘 prefab + 부착 부모")]
        [SerializeField] private GameObject _iconPrefab;
        [SerializeField] private Transform _iconRoot;

        [Header("배치")]
        [Tooltip("아이콘 1개 너비(canvas 단위). 가로 배치 시 i × spacing 으로 anchoredPosition.x 설정.")]
        [SerializeField] private float _spacing = 28f;

        [Header("스택 텍스트 옵션")]
        [Tooltip("스택이 1 이하이면 텍스트 숨김.")]
        [SerializeField] private bool _hideStackWhenOne = true;

        private IEventBus _eventBus;
        private IBuffSystem _buffSystem;
        private ISkillSystem _skillSystem;

        private readonly Dictionary<string, IconEntry> _entries = new Dictionary<string, IconEntry>();

        private System.Action<BuffAppliedEvent> _onApplied;
        private System.Action<BuffRemovedEvent> _onRemoved;
        private System.Action<BuffExpiredEvent> _onExpired;
        private System.Action<BuffRefreshedEvent> _onRefreshed;
        private System.Action<BuffStackChangedEvent> _onStackChanged;

        private void OnEnable()
        {
            if (_target == null) _target = GetComponentInParent<UnitController>();
            if (_iconRoot == null) _iconRoot = transform;

            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            _buffSystem = ServiceLocator.Has<IBuffSystem>() ? ServiceLocator.Get<IBuffSystem>() : null;
            _skillSystem = ServiceLocator.Has<ISkillSystem>() ? ServiceLocator.Get<ISkillSystem>() : null;

            if (_eventBus == null) return;

            _onApplied = OnApplied;
            _onRemoved = OnRemoved;
            _onExpired = OnExpired;
            _onRefreshed = OnRefreshed;
            _onStackChanged = OnStackChanged;

            _eventBus.Subscribe(_onApplied);
            _eventBus.Subscribe(_onRemoved);
            _eventBus.Subscribe(_onExpired);
            _eventBus.Subscribe(_onRefreshed);
            _eventBus.Subscribe(_onStackChanged);
        }

        private void OnDisable()
        {
            if (_eventBus != null)
            {
                if (_onApplied != null) _eventBus.Unsubscribe(_onApplied);
                if (_onRemoved != null) _eventBus.Unsubscribe(_onRemoved);
                if (_onExpired != null) _eventBus.Unsubscribe(_onExpired);
                if (_onRefreshed != null) _eventBus.Unsubscribe(_onRefreshed);
                if (_onStackChanged != null) _eventBus.Unsubscribe(_onStackChanged);
            }

            foreach (var kv in _entries)
            {
                if (kv.Value.Root != null) Destroy(kv.Value.Root);
            }
            _entries.Clear();
        }

        private void Update()
        {
            if (_target == null || _buffSystem == null) return;
            if (_entries.Count == 0) return;

            IReadOnlyList<IBuffInstance> active = _buffSystem.GetBuffs(_target.InstanceId);
            if (active == null) return;

            foreach (var kv in _entries)
            {
                IconEntry entry = kv.Value;
                IBuffInstance match = FindBuff(active, entry.BuffId, entry.SourceSkillId);
                if (match == null) continue;

                if (entry.Overlay != null)
                {
                    entry.Overlay.fillAmount = Mathf.Clamp01(match.RemainingDuration / Mathf.Max(0.0001f, entry.Duration));
                }
            }
        }

        private static IBuffInstance FindBuff(IReadOnlyList<IBuffInstance> list, string buffId, string sourceSkillId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                IBuffInstance b = list[i];
                if (b == null) continue;
                if (b.BuffId != buffId) continue;
                if (b.SourceSkillId != sourceSkillId) continue;
                return b;
            }
            return null;
        }

        private bool IsForMe(string targetId) => _target != null && targetId == _target.InstanceId;

        private static string MakeKey(string buffId, string sourceSkillId) => $"{buffId}|{sourceSkillId}";

        private void OnApplied(BuffAppliedEvent evt)
        {
            if (!IsForMe(evt.TargetId)) return;
            string key = MakeKey(evt.BuffId, evt.SourceSkillId);
            if (_entries.ContainsKey(key)) return;

            IconEntry entry = CreateIcon(evt.BuffId, evt.SourceSkillId, evt.Duration, evt.StackCount);
            if (entry != null)
            {
                _entries[key] = entry;
                RelayoutIcons();
            }
        }

        private void OnRemoved(BuffRemovedEvent evt)
        {
            if (!IsForMe(evt.TargetId)) return;
            RemoveIcon(MakeKey(evt.BuffId, evt.SourceSkillId));
        }

        private void OnExpired(BuffExpiredEvent evt)
        {
            if (!IsForMe(evt.TargetId)) return;
            RemoveIcon(MakeKey(evt.BuffId, evt.SourceSkillId));
        }

        private void OnRefreshed(BuffRefreshedEvent evt)
        {
            if (!IsForMe(evt.TargetId)) return;
            string key = MakeKey(evt.BuffId, evt.SourceSkillId);
            if (_entries.TryGetValue(key, out var entry))
            {
                entry.Duration = evt.NewDuration;
                if (entry.Overlay != null) entry.Overlay.fillAmount = 1f;
            }
        }

        private void OnStackChanged(BuffStackChangedEvent evt)
        {
            if (!IsForMe(evt.TargetId)) return;
            string key = MakeKey(evt.BuffId, evt.SourceSkillId);
            if (_entries.TryGetValue(key, out var entry))
            {
                UpdateStackText(entry, evt.NewStack);
            }
        }

        private IconEntry CreateIcon(string buffId, string sourceSkillId, float duration, int stack)
        {
            if (_iconPrefab == null || _iconRoot == null) return null;

            GameObject go = Instantiate(_iconPrefab, _iconRoot);
            go.name = $"buff_icon_{buffId}";

            Image iconImage = null;
            Image overlayImage = null;
            TMP_Text stackText = null;

            Transform iconT = go.transform.Find("Icon");
            if (iconT != null) iconImage = iconT.GetComponent<Image>();
            Transform overlayT = go.transform.Find("DurationOverlay");
            if (overlayT != null) overlayImage = overlayT.GetComponent<Image>();
            Transform stackT = go.transform.Find("StackText");
            if (stackT != null) stackText = stackT.GetComponent<TMP_Text>();

            if (iconImage != null && _skillSystem != null)
            {
                SkillData skill = _skillSystem.GetSkillData(sourceSkillId);
                if (skill != null && skill.Icon != null)
                {
                    iconImage.sprite = skill.Icon;
                    iconImage.enabled = true;
                }
            }

            if (overlayImage != null) overlayImage.fillAmount = 1f;

            var entry = new IconEntry
            {
                Root = go,
                Overlay = overlayImage,
                StackText = stackText,
                BuffId = buffId,
                SourceSkillId = sourceSkillId,
                Duration = duration <= 0f ? 1f : duration,
            };
            UpdateStackText(entry, stack);
            return entry;
        }

        private void RemoveIcon(string key)
        {
            if (!_entries.TryGetValue(key, out var entry)) return;
            if (entry.Root != null) Destroy(entry.Root);
            _entries.Remove(key);
            RelayoutIcons();
        }

        private void RelayoutIcons()
        {
            int i = 0;
            int n = _entries.Count;
            float offset = -(n - 1) * 0.5f * _spacing;
            foreach (var kv in _entries)
            {
                var rt = kv.Value.Root != null ? kv.Value.Root.transform as RectTransform : null;
                if (rt == null) { i++; continue; }
                Vector2 ap = rt.anchoredPosition;
                ap.x = offset + i * _spacing;
                ap.y = 0f;
                rt.anchoredPosition = ap;
                i++;
            }
        }

        private void UpdateStackText(IconEntry entry, int stack)
        {
            if (entry.StackText == null) return;
            if (_hideStackWhenOne && stack <= 1)
            {
                entry.StackText.text = string.Empty;
                entry.StackText.enabled = false;
            }
            else
            {
                entry.StackText.text = stack.ToString();
                entry.StackText.enabled = true;
            }
        }

        private class IconEntry
        {
            public GameObject Root;
            public Image Overlay;
            public TMP_Text StackText;
            public string BuffId;
            public string SourceSkillId;
            public float Duration;
        }
    }
}
