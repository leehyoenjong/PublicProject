using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PublicFramework
{
    /// <summary>
    /// 스킬 슬롯 1개의 UI 바인더.
    /// SkillData.Icon 을 _iconImage 에 자동 적용 + 쿨다운 진행 시 _cooldownOverlay.fillAmount 로 시각화.
    /// _button.onClick → caster.CastSkill 직접 호출 + 자동 타겟팅 (PC 클릭/모바일 터치 통합).
    /// SkillCooldownStartedEvent / SkillCooldownEndedEvent 로 진입/종료 트리거, Update 에서 CooldownRemaining polling.
    /// 자동 매핑: _caster 비어있으면 PlayerInputAdapter 부착 UnitController 자동 검색.
    /// _skill 비어있으면 caster.Unit as ICharacterInfo 의 BaseSkills[_slotIndex] 자동 조회.
    /// </summary>
    public class HudSkillSlotBinder : MonoBehaviour
    {
        [Header("바인딩 대상 (비워두면 자동 매핑)")]
        [SerializeField] private UnitController _caster;
        [SerializeField] private SkillData _skill;
        [SerializeField] private int _slotIndex;

        [Header("표시 컴포넌트")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _cooldownOverlay;
        [SerializeField] private TMP_Text _cooldownText;
        [SerializeField] private Button _button;

        [Header("표시 옵션")]
        [SerializeField] private string _cooldownFormat = "{0:F1}";

        private IEventBus _eventBus;
        private ISkillSystem _skillSystem;
        private Action<SkillCooldownStartedEvent> _onCooldownStarted;
        private Action<SkillCooldownEndedEvent> _onCooldownEnded;

        private float _currentDuration;
        private bool _isCoolingDown;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        private void OnEnable()
        {
            _eventBus = ServiceLocator.Has<IEventBus>() ? ServiceLocator.Get<IEventBus>() : null;
            _skillSystem = ServiceLocator.Has<ISkillSystem>() ? ServiceLocator.Get<ISkillSystem>() : null;

            TryAutoResolve();

            if (_eventBus != null)
            {
                _onCooldownStarted = OnCooldownStarted;
                _onCooldownEnded = OnCooldownEnded;
                _eventBus.Subscribe(_onCooldownStarted);
                _eventBus.Subscribe(_onCooldownEnded);
            }

            ResetCooldownVisual();
        }

        private void Start()
        {
            TryAutoResolve();
        }

        private void TryAutoResolve()
        {
            if (_caster == null)
            {
                _caster = FindOwnerUnit();
            }

            if (_skill == null && _caster != null && _caster.Unit != null)
            {
                _skill = ResolveSkillFromCaster(_caster, _slotIndex);
            }

            if (_iconImage != null && _skill != null && _skill.Icon != null)
            {
                _iconImage.sprite = _skill.Icon;
                _iconImage.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_eventBus == null) return;
            if (_onCooldownStarted != null) _eventBus.Unsubscribe(_onCooldownStarted);
            if (_onCooldownEnded != null) _eventBus.Unsubscribe(_onCooldownEnded);
            _isCoolingDown = false;
        }

        private void Update()
        {
            if (!_isCoolingDown) return;
            if (_skillSystem == null || _caster == null || _skill == null) return;

            ISkillInstance inst = _skillSystem.GetInstance(_caster.InstanceId, _skill.SkillId);
            if (inst == null || inst.CooldownRemaining <= 0f)
            {
                ResetCooldownVisual();
                return;
            }

            float ratio = _currentDuration > 0f ? Mathf.Clamp01(inst.CooldownRemaining / _currentDuration) : 0f;
            if (_cooldownOverlay != null) _cooldownOverlay.fillAmount = ratio;
            if (_cooldownText != null) _cooldownText.text = string.Format(_cooldownFormat, inst.CooldownRemaining);
        }

        private void OnCooldownStarted(SkillCooldownStartedEvent evt)
        {
            if (!IsMatch(evt.CasterId, evt.SkillId)) return;
            _currentDuration = evt.Duration;
            _isCoolingDown = evt.Duration > 0f;
            if (_cooldownOverlay != null) _cooldownOverlay.fillAmount = _isCoolingDown ? 1f : 0f;
            if (_cooldownText != null && _isCoolingDown) _cooldownText.text = string.Format(_cooldownFormat, evt.Duration);
        }

        private void OnCooldownEnded(SkillCooldownEndedEvent evt)
        {
            if (!IsMatch(evt.CasterId, evt.SkillId)) return;
            ResetCooldownVisual();
        }

        private void OnButtonClicked()
        {
            if (_caster == null || _skill == null || string.IsNullOrEmpty(_skill.SkillId)) return;
            if (!_caster.IsAlive) return;
            string targetId = FindNearestEnemyInstanceId(_caster);
            _caster.CastSkill(_skill.SkillId, targetId);
        }

        private static SkillData ResolveSkillFromCaster(UnitController caster, int slotIndex)
        {
            ICharacterInfo info = caster.Unit as ICharacterInfo;
            if (info == null) return null;
            IReadOnlyList<SkillData> skills = info.BaseSkills;
            if (skills == null || slotIndex < 0 || slotIndex >= skills.Count) return null;
            return skills[slotIndex];
        }

        private static string FindNearestEnemyInstanceId(UnitController self)
        {
            UnitController[] all = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            UnitController nearest = null;
            float nearestSqr = float.MaxValue;
            Vector3 origin = self.transform.position;

            foreach (UnitController u in all)
            {
                if (u == self) continue;
                if (!u.IsAlive) continue;
                float sqr = (u.transform.position - origin).sqrMagnitude;
                if (sqr >= nearestSqr) continue;
                nearestSqr = sqr;
                nearest = u;
            }

            return nearest != null ? nearest.InstanceId : null;
        }

        private bool IsMatch(string casterId, string skillId)
        {
            if (_caster == null || _skill == null) return false;
            return casterId == _caster.InstanceId && skillId == _skill.SkillId;
        }

        private void ResetCooldownVisual()
        {
            _isCoolingDown = false;
            _currentDuration = 0f;
            if (_cooldownOverlay != null) _cooldownOverlay.fillAmount = 0f;
            if (_cooldownText != null) _cooldownText.text = string.Empty;
        }

        private UnitController FindOwnerUnit()
        {
            UnitController[] all = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            foreach (UnitController u in all)
            {
                if (u != null && u.GetComponent<PlayerInputAdapter>() != null) return u;
            }
            return all.Length > 0 ? all[0] : null;
        }
    }
}
