namespace PublicFramework
{
    /// <summary>
    /// 엔티티별 IStatContainer 의 생성/조회/삭제. 컨테이너 자체가 스탯·재생·히스토리·스냅샷을 모두 담당.
    /// </summary>
    public interface IStatSystem : IService
    {
        IStatContainer CreateContainer(string ownerId, int level = 1);
        IStatContainer GetContainer(string ownerId);
        bool RemoveContainer(string ownerId);
        int Count { get; }

        /// <summary>등록된 모든 컨테이너에 일괄 Tick. 게임 루프에서 매 프레임 호출.</summary>
        void TickAll(float deltaTime);
    }
}
