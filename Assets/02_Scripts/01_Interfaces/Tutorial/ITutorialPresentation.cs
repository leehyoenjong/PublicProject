namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 연출 인터페이스. 교체 가능.
    /// </summary>
    public interface ITutorialPresentation
    {
        void ShowStep(TutorialStepData step, int stepIndex, int totalSteps);
        void HideStep();
        void ShowHighlight(TutorialStepData step);
        void HideHighlight();
        void ShowDialog(string text, DialogPosition position);
        void HideDialog();
        void ShowArrow(ArrowDirection direction);
        void HideArrow();
    }
}
