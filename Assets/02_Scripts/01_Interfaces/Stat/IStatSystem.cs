using System;

namespace PublicFramework
{
    /// <summary>
    /// 엔티티별 스탯 컨테이너를 관리하는 서비스 인터페이스
    /// </summary>
    public interface IStatSystem : IService
    {
        IStatContainer CreateContainer(string ownerId);
        IStatContainer GetContainer(string ownerId);
        bool RemoveContainer(string ownerId);
    }
}
