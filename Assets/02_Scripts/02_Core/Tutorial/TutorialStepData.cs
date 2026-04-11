using System;
using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 스텝 데이터. 직렬화 가능.
    /// </summary>
    [Serializable]
    public class TutorialStepData
    {
        [SerializeField] private TutorialStepType _stepType;
        [SerializeField] private string _dialogText;
        [SerializeField] private DialogPosition _dialogPosition;
        [SerializeField] private string _highlightTargetId;
        [SerializeField] private HighlightShape _highlightShape;
        [SerializeField] private ArrowDirection _arrowDirection;
        [SerializeField] private StepWaitType _waitType;
        [SerializeField] private float _waitDuration;
        [SerializeField] private string _waitConditionId;
        [SerializeField] private bool _canSkip;

        public TutorialStepType StepType => _stepType;
        public string DialogText => _dialogText;
        public DialogPosition DialogPosition => _dialogPosition;
        public string HighlightTargetId => _highlightTargetId;
        public HighlightShape HighlightShape => _highlightShape;
        public ArrowDirection ArrowDirection => _arrowDirection;
        public StepWaitType WaitType => _waitType;
        public float WaitDuration => _waitDuration;
        public string WaitConditionId => _waitConditionId;
        public bool CanSkip => _canSkip;
    }
}
