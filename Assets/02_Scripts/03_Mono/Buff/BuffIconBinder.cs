using UnityEngine;

namespace PublicFramework
{
    /// <summary>
    /// BuffIconBar에 엔티티 ID를 Inspector에서 연결. 디자이너가 코드 없이 쓰도록 감쌈.
    /// </summary>
    [RequireComponent(typeof(BuffIconBar))]
    public class BuffIconBinder : MonoBehaviour
    {
        [SerializeField] private string _ownerId;

        private BuffIconBar _bar;

        private void Awake()
        {
            _bar = GetComponent<BuffIconBar>();
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_ownerId))
            {
                _bar.Init(_ownerId);
            }
        }

        public void SetOwner(string ownerId)
        {
            _ownerId = ownerId;
            if (_bar != null)
            {
                _bar.Init(ownerId);
            }
        }
    }
}
