namespace PublicFramework
{
    /// <summary>
    /// 배너 해금 조건 / 뽑기 자격 체크용 플레이어 상태 공급자.
    /// Shop 의 IShopContext 와 의도적으로 분리 — 프로젝트별 확장 방향이 다를 수 있음.
    /// </summary>
    public interface IGachaContext
    {
        int PlayerLevel { get; }
        bool IsQuestCleared(int questMID);
    }
}
