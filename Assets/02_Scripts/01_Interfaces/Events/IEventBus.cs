using System;

namespace PublicFramework
{
    /// <summary>
    /// 제네릭 타입 기반 이벤트 버스 인터페이스.
    /// 시스템 간 느슨한 결합을 위한 Pub/Sub 패턴.
    /// </summary>
    public interface IEventBus : IService
    {
        void Subscribe<T>(Action<T> handler) where T : struct;
        void Unsubscribe<T>(Action<T> handler) where T : struct;
        void Publish<T>(T eventData) where T : struct;
        void Clear();
        void Clear<T>() where T : struct;
    }
}
