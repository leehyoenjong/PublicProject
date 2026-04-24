namespace PublicFramework
{
    /// <summary>
    /// 가챠별 천장 카운터 상태. IGachaRepository 가 영속화.
    /// Soft/Hard 는 SSR 미획득 뽑기 횟수, Pickup 은 픽업 미획득 뽑기 횟수.
    /// </summary>
    public interface IPityCounter
    {
        string GachaMID { get; }
        int PullsSinceLastSSR { get; }
        int PullsSinceLastPickup { get; }
        int TotalPullCount { get; }
        long LastPullAtUtc { get; }
    }
}
