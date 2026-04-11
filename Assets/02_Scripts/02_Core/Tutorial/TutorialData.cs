using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 정의 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "PublicFramework/Tutorial/TutorialData")]
    public class TutorialData : ScriptableObject
    {
        [Header("기본 정보")]
        [SerializeField] private string _tutorialId;
        [SerializeField] private string _displayName;
        [SerializeField] private int _priority;
        [SerializeField] private bool _canSkip;

        [Header("트리거")]
        [SerializeField] private TriggerType _triggerType;
        [SerializeField] private string _triggerValue;

        [Header("선행 조건")]
        [SerializeField] private string[] _prerequisiteTutorialIds;

        [Header("스텝")]
        [SerializeField] private TutorialStepData[] _steps;

        public string TutorialId => _tutorialId;
        public string DisplayName => _displayName;
        public int Priority => _priority;
        public bool CanSkip => _canSkip;
        public TriggerType TriggerType => _triggerType;
        public string TriggerValue => _triggerValue;
        public IReadOnlyList<string> PrerequisiteTutorialIds => _prerequisiteTutorialIds;
        public IReadOnlyList<TutorialStepData> Steps => _steps;
    }
}
