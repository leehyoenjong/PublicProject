namespace PublicFramework
{
    /// <summary>
    /// 확률 판정 인터페이스.
    /// 천장 시스템 포함 확률 계산을 추상화한다.
    /// </summary>
    public interface IProbabilityModel
    {
        bool Roll(float baseProb, int pityCount, int maxPity);
        float GetDisplayProb(float baseProb, int pityCount, int maxPity);
    }
}
