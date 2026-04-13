using UnityEngine;
using UnityEngine.Events;

namespace PublicFramework
{
    /// <summary>
    /// GameEventSO 수신자. Inspector에서 UnityEvent에 반응 연결.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEventSO _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_event != null)
            {
                _event.Register(OnEventRaised);
            }
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.Unregister(OnEventRaised);
            }
        }

        private void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
