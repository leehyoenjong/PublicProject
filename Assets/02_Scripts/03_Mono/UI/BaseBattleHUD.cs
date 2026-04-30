using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    public class BaseBattleHUD : BaseOverlay, IBattleHUD
    {
        [SerializeField] private Transform _widgetRoot;

        private readonly List<IBattleWidget> _widgets = new List<IBattleWidget>();
        private BattleHUDConfig _config;

        public void SetupHUD(BattleHUDConfig config)
        {
            _config = config;

            ClearWidgets();

            if (config.CustomWidgets == null) return;

            foreach (WidgetEntry entry in config.CustomWidgets)
            {
                if (!entry.Enabled || entry.Prefab == null) continue;

                Transform parent = _widgetRoot != null ? _widgetRoot : transform;
                GameObject widgetObj = Instantiate(entry.Prefab, parent);

                if (widgetObj.TryGetComponent<IBattleWidget>(out var widget))
                {
                    widget.Init();
                    widget.Show();
                    _widgets.Add(widget);
                    Debug.Log($"[UI] 위젯 '{entry.Id}' 추가됨.");
                }
                else
                {
                    Debug.LogWarning($"[전투HUD] 위젯 '{entry.Id}'에 IBattleWidget 컴포넌트 없음.");
                }
            }

            Show();
            Debug.Log($"[전투HUD] HUD 셋업 완료. 위젯 수: {_widgets.Count}");
        }

        public virtual void UpdateHP(string unitId, float ratio)
        {
            Debug.Log($"[전투HUD] HP 갱신: {unitId} → {ratio:P0}");
        }

        public virtual void UpdateSkill(string skillId, float coolRatio)
        {
            Debug.Log($"[전투HUD] 스킬 갱신: {skillId} → {coolRatio:P0}");
        }

        public virtual void ShowDamageText(Vector3 position, int value, DamageType type)
        {
            Debug.Log($"[전투HUD] 데미지: {value} ({type}) 위치 {position}");
        }

        public virtual void UpdateWave(int current, int total)
        {
            Debug.Log($"[전투HUD] 웨이브: {current}/{total}");
        }

        public void Cleanup()
        {
            ClearWidgets();
            Hide();
            Debug.Log("[전투HUD] 정리 완료.");
        }

        private void ClearWidgets()
        {
            foreach (IBattleWidget widget in _widgets)
            {
                widget.Hide();

                if (widget is MonoBehaviour mb && mb != null)
                {
                    Destroy(mb.gameObject);
                }
            }

            _widgets.Clear();
        }

        protected override void Awake()
        {
            base.Awake();
        }

        private void OnDestroy()
        {
            ClearWidgets();
        }
    }
}
