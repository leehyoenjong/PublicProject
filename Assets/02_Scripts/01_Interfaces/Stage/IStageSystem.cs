using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 스테이지 시스템 서비스 인터페이스.
    /// 챕터·스테이지 등록, 입장 검증, 진행 관리, 이벤트 트리거, 보상 처리.
    /// </summary>
    public interface IStageSystem : IService
    {
        StageConfig Config { get; }

        void RegisterChapter(ChapterData chapterData);
        void RegisterStage(StageData stageData);

        bool CanEnter(string stageId, int playerLevel);
        bool TryEnter(StageContext context, int playerLevel);
        bool TrySweep(string stageId, int playerLevel, int times);

        void ReportWaveCleared();
        void ReportHpThreshold(float currentHpRatio);
        void ReportStageWin(int starsAchieved);
        void ReportStageFail(StageLoseCondition reason);
        void Tick(float deltaTime);

        void TriggerManualEvent(int eventIndex);
        void CompleteEvent(int eventIndex);

        StageInstance GetInstance(string stageId);
        IReadOnlyList<StageInstance> GetStagesByChapter(string chapterId);
        bool IsChapterCompleted(string chapterId);
        void CheckUnlocks(int playerLevel);

        void SetRewardHandler(IRewardHandler handler);
    }
}
