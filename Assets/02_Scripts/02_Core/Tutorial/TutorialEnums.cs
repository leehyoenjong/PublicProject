namespace PublicFramework
{
    public enum TutorialStepType
    {
        Dialog,
        Highlight,
        Action,
        Wait,
        Custom
    }

    public enum TriggerType
    {
        FirstLogin,
        LevelReach,
        QuestComplete,
        StageEnter,
        UIOpen,
        ItemGet,
        Manual,
        ConditionMet,
        ButtonClick,
        StageClear,
        AchievementUnlocked
    }

    public enum StepWaitType
    {
        Tap,
        ButtonClick,
        Timer,
        Condition,
        None
    }

    public enum HighlightShape
    {
        Circle,
        Rectangle,
        None
    }

    public enum ArrowDirection
    {
        Up,
        Down,
        Left,
        Right,
        None
    }

    public enum DialogPosition
    {
        Top,
        Center,
        Bottom
    }
}
