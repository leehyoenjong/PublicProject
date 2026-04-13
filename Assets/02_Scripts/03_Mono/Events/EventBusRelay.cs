using UnityEngine;
using UnityEngine.Events;

namespace PublicFramework
{
    /// <summary>
    /// EventBus의 struct 이벤트를 UnityEvent로 중계하는 추상 베이스.
    /// 구체 이벤트별로 subclass를 만들어 씬에서 Inspector로 연결.
    /// </summary>
    public abstract class EventBusRelay<T> : MonoBehaviour where T : struct
    {
        [SerializeField] private UnityEvent _response;

        private IEventBus _eventBus;

        protected virtual void Start()
        {
            _eventBus = ServiceLocator.Get<IEventBus>();
            _eventBus?.Subscribe<T>(OnEvent);
        }

        protected virtual void OnDestroy()
        {
            _eventBus?.Unsubscribe<T>(OnEvent);
        }

        private void OnEvent(T evt)
        {
            OnEventReceived(evt);
            _response?.Invoke();
        }

        /// <summary>
        /// 구체 subclass가 evt 내용을 활용해 로직을 추가할 수 있는 훅.
        /// </summary>
        protected virtual void OnEventReceived(T evt)
        {
        }
    }

    /// <summary>
    /// 예시: 퀘스트 완료 시 UnityEvent 발행.
    /// </summary>
    public class QuestCompletedRelay : EventBusRelay<QuestCompletedEvent>
    {
    }
}
