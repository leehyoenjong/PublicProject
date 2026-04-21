namespace PublicFramework
{
    /// <summary>
    /// SkillAction.Spawn 이 투사체 프리팹 인스턴스에 발화자/레벨 정보를 주입할 때 사용.
    /// 투사체 프리팹의 MonoBehaviour 가 이 인터페이스를 구현하면 자동 호출된다.
    /// </summary>
    public interface IProjectileInit
    {
        void Initialize(string casterId, int level, float powerMultiplier);
    }
}
