using System.Collections.Generic;

namespace PublicFramework
{
    /// <summary>
    /// 튜토리얼 시스템 서비스 인터페이스
    /// </summary>
    public interface ITutorialSystem : IService
    {
        void StartTutorial(string tutorialId);
        void NextStep();
        void SkipTutorial();
        void CompleteTutorial(string tutorialId);
        bool IsTutorialCompleted(string tutorialId);
        bool IsRunning { get; }
        string CurrentTutorialId { get; }
        int CurrentStepIndex { get; }
        void CheckTriggers();
        IReadOnlyList<string> GetCompletedTutorials();
    }
}
