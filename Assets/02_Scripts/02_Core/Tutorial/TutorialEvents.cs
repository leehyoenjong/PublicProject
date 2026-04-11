namespace PublicFramework
{
    public struct TutorialStartedEvent
    {
        public string TutorialId;
        public int TotalSteps;
    }

    public struct TutorialStepChangedEvent
    {
        public string TutorialId;
        public int StepIndex;
        public TutorialStepType StepType;
    }

    public struct TutorialCompletedEvent
    {
        public string TutorialId;
    }

    public struct TutorialSkippedEvent
    {
        public string TutorialId;
        public int SkippedAtStep;
    }

    public struct TutorialTriggeredEvent
    {
        public string TutorialId;
        public TriggerType TriggerType;
    }
}
