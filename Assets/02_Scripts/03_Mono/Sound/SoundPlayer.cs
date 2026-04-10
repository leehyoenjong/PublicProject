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
                Debug.LogWarning("[SoundPlayer] No sound entries configured.");
                return;
            }

            foreach (SoundData entry in _soundEntries)
            {
                if (entry.Clip == null)
                {
                    Debug.LogError($"[SoundPlayer] AudioClip is null for entry '{entry.Id}'. Skipping.");
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Id))
                {
                    entry.Id = entry.Clip.name;
                }

                _soundManager.RegisterSound(entry);
            }

            Debug.Log($"[SoundPlayer] Initialized with {_soundEntries.Length} sound(s).");
        }

        private void OnDestroy()
        {
            _soundManager?.StopAll();
            ServiceLocator.Unregister<ISoundManager>();
            Debug.Log("[SoundPlayer] Destroyed. All sounds stopped.");
        }
    }
}
