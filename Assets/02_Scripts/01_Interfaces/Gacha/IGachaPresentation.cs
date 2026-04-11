namespace PublicFramework
{
    /// <summary>
    /// 가챠 연출 인터페이스
    /// </summary>
    public interface IGachaPresentation
    {
        void PlayPullAnimation(int count);
        void ShowResult(GachaReward[] rewards);
        void UpdatePityCounter(PityCounter pityCounter);
    }
}
