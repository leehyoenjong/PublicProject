namespace PublicFramework
{
    /// <summary>
    /// 씬 간 "선택된 스테이지" 운반자. 로비(스테이지 선택 허브)가 Select 로 stageId 를 기록하고,
    /// 전투 씬의 StageBattleHost 가 진입 시 SelectedStageId 를 읽는다.
    /// ServiceLocator 에 등록되어 씬 전환을 가로질러 생존한다(INIT DontDestroyOnLoad).
    /// 미설정(null/빈값)이면 StageBattleHost 는 자신의 직렬화 기본값으로 폴백한다.
    /// </summary>
    public interface IStageSelection : IService
    {
        string SelectedStageId { get; }
        void Select(string stageId);
        void Clear();
    }
}
