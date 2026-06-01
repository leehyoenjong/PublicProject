namespace PublicFramework
{
    /// <summary>
    /// 씬 간 "선택된 스테이지" 운반자. 로비(스테이지 선택 허브)가 Select 로 stageId 를 기록하고,
    /// 전투 씬의 StageBattleHost 가 진입 시 SelectedStageId 를 읽는다.
    /// ServiceLocator 에 등록되어 씬 전환을 가로질러 생존한다(INIT DontDestroyOnLoad).
    /// 미설정(null/빈값)이면 StageBattleHost 는 자신의 직렬화 기본값으로 폴백한다.
    ///
    /// 진입 정책은 파생 게임의 선택사항 — 이 캐리어는 "선택" 모드를 위한 중립 운반자일 뿐 선택을 강제하지 않는다:
    ///  1. 명시 선택 — 로비 허브(StageEnterButton 샘플)가 Select 호출
    ///  2. 고정/현재 스테이지 — 아무도 Select 안 하면 호스트의 직렬화 _stageId 로 진입(선택 화면 없는 게임)
    ///  3. 이어하기 — 게임이 진행도/세이브에서 "현재 스테이지"를 읽어 Select 호출
    /// </summary>
    public interface IStageSelection : IService
    {
        string SelectedStageId { get; }
        void Select(string stageId);
        void Clear();
    }
}
