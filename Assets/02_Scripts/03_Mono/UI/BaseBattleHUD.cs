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
                    Debug.Log($"[BaseBattleHUD] Widget '{entry.Id}' added.");
                }
                else
                {
                    Debug.LogWarning($"[BaseBattleHUD] Widget '{entry.Id}' has no IBattleWidget component.");
                }
            }

            Show();
            Debug.Log($"[BaseBattleHUD] HUD setup complete. Widgets: {_widgets.Count}");
        }

        public virtual void UpdateHP(string unitId, float ratio)
        {
            Debug.Log($"[BaseBattleHUD] HP updated: {unitId} -> {ratio:P0}");
        }

        public virtual void UpdateSkill(string skillId, float coolRatio)
        {
            Debug.Log($"[BaseBattleHUD] Skill updated: {skillId} -> {coolRatio:P0}");
        }

        public virtual void ShowDamageText(Vector3 position, int value, DamageType type)
        {
            Debug.Log($"[BaseBattleHUD] Damage: {value} ({type}) at {position}");
        }

        public virtual void UpdateWave(int current, int total)
        {
            Debug.Log($"[BaseBattleHUD] Wave: {current}/{total}");
        }

        public void Cleanup()
        {
            ClearWidgets();
            Hide();
            Debug.Log("[BaseBattleHUD] Cleanup complete.");
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
