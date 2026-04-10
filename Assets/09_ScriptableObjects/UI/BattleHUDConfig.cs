using System;
using UnityEngine;

namespace PublicFramework
{
    [CreateAssetMenu(fileName = "BattleHUDConfig", menuName = "PublicFramework/UI/BattleHUDConfig")]
    public class BattleHUDConfig : ScriptableObject
    {
        [SerializeField] private bool _showHPBar = true;
        [SerializeField] private bool _showSkillBar = true;
        [SerializeField] private bool _showWaveInfo = true;
        [SerializeField] private bool _showDamageText = true;
        [SerializeField] private bool _showMiniMap;
        [SerializeField] private bool _showComboCounter;
        [SerializeField] private WidgetEntry[] _customWidgets;

        public bool ShowHPBar => _showHPBar;
        public bool ShowSkillBar => _showSkillBar;
        public bool ShowWaveInfo => _showWaveInfo;
        public bool ShowDamageText => _showDamageText;
        public bool ShowMiniMap => _showMiniMap;
        public bool ShowComboCounter => _showComboCounter;
        public WidgetEntry[] CustomWidgets => _customWidgets;
    }

    [Serializable]
    public class WidgetEntry
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private bool _enabled = true;

        public string Id => _id;
        public GameObject Prefab => _prefab;
        public bool Enabled => _enabled;
    }
}
