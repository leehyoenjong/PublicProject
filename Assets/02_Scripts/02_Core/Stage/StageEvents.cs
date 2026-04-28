namespace PublicFramework
{
    public struct StageRegisteredEvent
    {
        public string StageId;
    }

    public struct StageUnlockedEvent
    {
        public string StageId;
    }

    public struct StageEnteredEvent
    {
        public string StageId;
        public bool IsSweep;
        public bool AutoBattle;
    }

    public struct StageClearedEvent
    {
        public string StageId;
        public bool IsFirstClear;
        public int Stars;
        public float ElapsedSeconds;
    }

    public struct StageFailedEvent
    {
        public string StageId;
        public StageLoseCondition Reason;
        public float ElapsedSeconds;
    }

    public struct StageSweptEvent
    {
        public string StageId;
        public int SweepCount;
    }

    public struct WaveStartedEvent
    {
        public string StageId;
        public int WaveIndex;
    }

    public struct WaveClearedEvent
    {
        public string StageId;
        public int WaveIndex;
    }

    public struct StageEventTriggeredEvent
    {
        public string StageId;
        public int EventIndex;
        public StageEventType EventType;
        public string TargetId;
    }

    public struct StageEventCompletedEvent
    {
        public string StageId;
        public int EventIndex;
        public StageEventType EventType;
        public string TargetId;
    }

    public struct ChapterCompletedEvent
    {
        public string ChapterId;
    }
}
