using UnityEngine;

namespace PublicFramework
{
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField] private SoundData[] _soundEntries;
        [SerializeField] private int _sfxPoolSize = 10;

        private SoundManager _soundManager;

        public ISoundManager SoundManager => _soundManager;

        private void Awake()
        {
            _soundManager = new SoundManager(this, transform, _sfxPoolSize);
            ServiceLocator.Register<ISoundManager>(_soundManager);

            if (_soundEntries == null || _soundEntries.Length == 0)
            {
                Debug.LogWarning("[사운드] 등록된 사운드 항목 없음.");
                return;
            }

            foreach (SoundData entry in _soundEntries)
            {
                if (entry.Clip == null)
                {
                    Debug.LogError($"[사운드] 항목 '{entry.Id}'의 AudioClip이 null임. 건너뜀.");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = entry.Clip.name;
                }

                _soundManager.RegisterSound(entry);
            }

            Debug.Log($"[사운드] {_soundEntries.Length}개 사운드로 초기화됨.");
        }

        private void OnDestroy()
        {
            _soundManager?.StopAll();
            ServiceLocator.Unregister<ISoundManager>();
            Debug.Log("[사운드] 제거됨. 모든 사운드 정지됨.");
        }
    }
}
