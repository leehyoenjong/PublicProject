namespace PublicFramework.Tests
{
    /// <summary>테스트용 ITutorialPresentation. 호출 횟수와 마지막 인자 기록.</summary>
    public class FakeTutorialPresentation : ITutorialPresentation
    {
        public int ShowStepCalls { get; private set; }
        public int HideStepCalls { get; private set; }
        public int ShowHighlightCalls { get; private set; }
        public int HideHighlightCalls { get; private set; }
        public int ShowDialogCalls { get; private set; }
        public int HideDialogCalls { get; private set; }
        public int ShowArrowCalls { get; private set; }
        public int HideArrowCalls { get; private set; }

        public TutorialStepData LastStep { get; private set; }
        public int LastStepIndex { get; private set; }
        public int LastTotalSteps { get; private set; }
        public int LastDialogTextKey { get; private set; }
        public DialogPosition LastDialogPosition { get; private set; }
        public ArrowDirection LastArrowDirection { get; private set; }

        public void ShowStep(TutorialStepData step, int stepIndex, int totalSteps)
        {
            ShowStepCalls++;
            LastStep = step;
            LastStepIndex = stepIndex;
            LastTotalSteps = totalSteps;
        }

        public void HideStep() { HideStepCalls++; }
        public void ShowHighlight(TutorialStepData step) { ShowHighlightCalls++; }
        public void HideHighlight() { HideHighlightCalls++; }

        public void ShowDialog(int dialogTextKey, DialogPosition position)
        {
            ShowDialogCalls++;
            LastDialogTextKey = dialogTextKey;
            LastDialogPosition = position;
        }

        public void HideDialog() { HideDialogCalls++; }

        public void ShowArrow(ArrowDirection direction)
        {
            ShowArrowCalls++;
            LastArrowDirection = direction;
        }

        public void HideArrow() { HideArrowCalls++; }
    }
}
