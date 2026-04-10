using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PublicFramework
{
    public class SoundManager : ISoundManager
    {
        private const int DEFAULT_SFX_POOL_SIZE = 10;

        private readonly Dictionary<string, SoundData> _soundTable = new Dictionary<string, SoundData>();
        private readonly Dictionary<string, int> _sfxPlayCount = new Dictionary<string, int>();
        private readonly Dictionary<AudioSource, float> _sfxBaseVolumes = new Dictionary<AudioSource, float>();
        private readonly Dictionary<AudioSource, string> _sfxSourceToId = new Dictionary<AudioSource, string>();
        private readonly Dictionary<string, List<AudioSource>> _sfxIdToSources = new Dictionary<string, List<AudioSource>>();

        private readonly AudioSource _bgmSourceA;
        private readonly AudioSource _bgmSourceB;
        private readonly AudioSource _voiceSource;
        private readonly AudioSource[] _sfxSources;
        private readonly MonoBehaviour _coroutineRunner;

        private AudioSource _activeBgmSource;
        private string _currentBgmId;
        private float _currentBgmDataVolume;
        private float _currentVoiceDataVolume;
        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;
        private float _voiceVolume = 1f;

        private bool _bgmMuted;
        private bool _sfxMuted;
        private bool _voiceMuted;

        public float MasterVolume => _masterVolume;
        public float BGMVolume => _bgmVolume;
        public float SFXVolume => _sfxVolume;
        public float VoiceVolume => _voiceVolume;

        public SoundManager(MonoBehaviour coroutineRunner, Transform parent, int sfxPoolSize = DEFAULT_SFX_POOL_SIZE)
        {
            _coroutineRunner = coroutineRunner;

            _bgmSourceA = CreateAudioSource("BGM_A", parent);
            _bgmSourceB = CreateAudioSource("BGM_B", parent);
            _activeBgmSource = _bgmSourceA;

            _voiceSource = CreateAudioSource("Voice", parent);

            _sfxSources = new AudioSource[sfxPoolSize];
            for (int i = 0; i < sfxPoolSize; i++)
            {
                _sfxSources[i] = CreateAudioSource($"SFX_{i}", parent);
            }

            Debug.Log($"[SoundManager] Init started. SFX pool: {sfxPoolSize}");
        }

        public void RegisterSound(SoundData data)
        {
            if (string.IsNullOrEmpty(data.Id))
            {
                Debug.LogError("[SoundManager] SoundData Id is null or empty.");
                return;
            }

            _soundTable[data.Id] = data;
            Debug.Log($"[SoundManager] Sound registered: {data.Id} ({data.Channel})");
        }

        // --- BGM ---

        public void PlayBGM(string id, float fadeTime = 1f)
        {
            if (!TryGetSound(id, out SoundData data)) return;

            if (_currentBgmId == id) return;

            AudioSource nextSource = (_activeBgmSource == _bgmSourceA) ? _bgmSourceB : _bgmSourceA;

            nextSource.clip = data.Clip;
            nextSource.loop = true;
            nextSource.priority = data.Priority;
            nextSource.mute = _bgmMuted;
            nextSource.Play();

            _currentBgmDataVolume = data.Volume;
            _coroutineRunner.StartCoroutine(CrossFadeBGM(_activeBgmSource, nextSource, fadeTime, data.Volume));

            _activeBgmSource = nextSource;
            _currentBgmId = id;

            Debug.Log($"[SoundManager] BGM play: {id}, FadeTime: {fadeTime}");
        }

        public void StopBGM(float fadeTime = 1f)
        {
            _coroutineRunner.StartCoroutine(FadeOutSource(_activeBgmSource, fadeTime));
            _currentBgmId = null;
            _currentBgmDataVolume = 0f;
            Debug.Log("[SoundManager] BGM stopped.");
        }

        // --- SFX ---

        public void PlaySFX(string id)
        {
            if (!TryGetSound(id, out SoundData data)) return;

            switch (data.Policy)
            {
                case SFXPolicy.RejectIfPlaying:
                    if (IsSFXPlaying(id)) return;
                    break;

                case SFXPolicy.RestartIfPlaying:
                    StopSFX(id);
                    break;

                case SFXPolicy.LimitCount:
                    _sfxPlayCount.TryGetValue(id, out int count);
                    if (count >= data.MaxCount)
                    {
                        StopOldestSFX(id);
                    }
                    break;
            }

            AudioSource source = GetAvailableSFXSource(data.Priority);
            if (source == null)
            {
                Debug.LogWarning($"[SoundManager] SFX pool exhausted. Skipping: {id}");
                return;
            }

            source.clip = data.Clip;
            source.volume = data.Volume * _sfxVolume * _masterVolume;
            source.loop = data.Loop;
            source.priority = data.Priority;
            source.mute = _sfxMuted;
            source.Play();

            _sfxBaseVolumes[source] = data.Volume;
            RegisterSFXSource(source, id);

            _sfxPlayCount.TryGetValue(id, out int current);
            _sfxPlayCount[id] = current + 1;

            if (!data.Loop)
            {
                _coroutineRunner.StartCoroutine(TrackSFXEnd(source, id, data.Clip.length));
            }

            Debug.Log($"[SoundManager] SFX play: {id}");
        }

        public void StopSFX(string id)
        {
            if (!TryGetSound(id, out SoundData data)) return;

            foreach (AudioSource source in _sfxSources)
            {
                if (source.isPlaying && source.clip == data.Clip)
                {
                    source.Stop();
                    source.clip = null;
                    _sfxBaseVolumes.Remove(source);
                    UnregisterSFXSource(source);
                }
            }

            _sfxPlayCount.Remove(id);
            Debug.Log($"[SoundManager] SFX stopped: {id}");
        }

        // --- Voice ---

        public void PlayVoice(string id)
        {
            if (!TryGetSound(id, out SoundData data)) return;

            _voiceSource.Stop();
            _voiceSource.clip = data.Clip;
            _voiceSource.volume = data.Volume * _voiceVolume * _masterVolume;
            _voiceSource.loop = false;
            _voiceSource.priority = data.Priority;
            _voiceSource.mute = _voiceMuted;
            _voiceSource.Play();

            _currentVoiceDataVolume = data.Volume;

            Debug.Log($"[SoundManager] Voice play: {id}");
        }

        public void StopVoice()
        {
            _voiceSource.Stop();
            _voiceSource.clip = null;
            _currentVoiceDataVolume = 0f;
            Debug.Log("[SoundManager] Voice stopped.");
        }

        // --- Stop All ---

        public void StopAll()
        {
            StopBGM(0f);
            StopVoice();

            foreach (AudioSource source in _sfxSources)
            {
                source.Stop();
                source.clip = null;
            }

            _sfxPlayCount.Clear();
            _sfxBaseVolumes.Clear();
            _sfxSourceToId.Clear();
            _sfxIdToSources.Clear();
            Debug.Log("[SoundManager] All sounds stopped.");
        }

        // --- Volume ---

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            Debug.Log($"[SoundManager] Master volume: {_masterVolume}");
        }

        public void SetBGMVolume(float volume)
        {
            _bgmVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            Debug.Log($"[SoundManager] BGM volume: {_bgmVolume}");
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            Debug.Log($"[SoundManager] SFX volume: {_sfxVolume}");
        }

        public void SetVoiceVolume(float volume)
        {
            _voiceVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
            Debug.Log($"[SoundManager] Voice volume: {_voiceVolume}");
        }

        // --- Mute ---

        public void SetMute(SoundChannel channel, bool mute)
        {
            switch (channel)
            {
                case SoundChannel.BGM:
                    _bgmMuted = mute;
                    _bgmSourceA.mute = mute;
                    _bgmSourceB.mute = mute;
                    break;

                case SoundChannel.SFX:
                    _sfxMuted = mute;
                    foreach (AudioSource source in _sfxSources)
                    {
                        source.mute = mute;
                    }
                    break;

                case SoundChannel.Voice:
                    _voiceMuted = mute;
                    _voiceSource.mute = mute;
                    break;
            }

            Debug.Log($"[SoundManager] {channel} mute: {mute}");
        }

        public bool IsMuted(SoundChannel channel)
        {
            return channel switch
            {
                SoundChannel.BGM => _bgmMuted,
                SoundChannel.SFX => _sfxMuted,
                SoundChannel.Voice => _voiceMuted,
                _ => false
            };
        }

        // --- Private ---

        private void ApplyVolumes()
        {
            float bgmFinalVolume = _currentBgmDataVolume * _bgmVolume * _masterVolume;
            if (_activeBgmSource.isPlaying)
            {
                _activeBgmSource.volume = bgmFinalVolume;
            }

            foreach (AudioSource source in _sfxSources)
            {
                if (source.isPlaying && _sfxBaseVolumes.TryGetValue(source, out float baseVolume))
                {
                    source.volume = baseVolume * _sfxVolume * _masterVolume;
                }
            }

            if (_voiceSource.isPlaying)
            {
                _voiceSource.volume = _currentVoiceDataVolume * _voiceVolume * _masterVolume;
            }
        }

        private void StopOldestSFX(string id)
        {
            if (!_sfxIdToSources.TryGetValue(id, out List<AudioSource> sources) || sources.Count == 0)
            {
                return;
            }

            AudioSource oldest = sources[0];
            oldest.Stop();
            oldest.clip = null;
            _sfxBaseVolumes.Remove(oldest);
            UnregisterSFXSource(oldest);

            _sfxPlayCount.TryGetValue(id, out int count);
            count--;
            if (count <= 0)
                _sfxPlayCount.Remove(id);
            else
                _sfxPlayCount[id] = count;

            Debug.Log($"[SoundManager] Stopped oldest SFX: {id}");
        }

        private void RegisterSFXSource(AudioSource source, string id)
        {
            _sfxSourceToId[source] = id;

            if (!_sfxIdToSources.TryGetValue(id, out List<AudioSource> sources))
            {
                sources = new List<AudioSource>();
                _sfxIdToSources[id] = sources;
            }

            sources.Add(source);
        }

        private void UnregisterSFXSource(AudioSource source)
        {
            if (_sfxSourceToId.TryGetValue(source, out string id))
            {
                _sfxSourceToId.Remove(source);

                if (_sfxIdToSources.TryGetValue(id, out List<AudioSource> sources))
                {
                    sources.Remove(source);
                    if (sources.Count == 0)
                    {
                        _sfxIdToSources.Remove(id);
                    }
                }
            }
        }

        private AudioSource GetAvailableSFXSource(int priority)
        {
            foreach (AudioSource source in _sfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            AudioSource lowest = null;
            int lowestPriority = priority;

            foreach (AudioSource source in _sfxSources)
            {
                if (source.priority > lowestPriority)
                {
                    lowestPriority = source.priority;
                    lowest = source;
                }
            }

            if (lowest != null)
            {
                lowest.Stop();
                _sfxBaseVolumes.Remove(lowest);
                UnregisterSFXSource(lowest);
                return lowest;
            }

            return null;
        }

        private bool IsSFXPlaying(string id)
        {
            if (!_soundTable.TryGetValue(id, out SoundData data)) return false;

            foreach (AudioSource source in _sfxSources)
            {
                if (source.isPlaying && source.clip == data.Clip)
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryGetSound(string id, out SoundData data)
        {
            if (_soundTable.TryGetValue(id, out data))
            {
                return true;
            }

            Debug.LogError($"[SoundManager] Sound '{id}' not registered.");
            data = null;
            return false;
        }

        private AudioSource CreateAudioSource(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            return go.AddComponent<AudioSource>();
        }

        private IEnumerator CrossFadeBGM(AudioSource from, AudioSource to, float duration, float targetVolume)
        {
            float elapsed = 0f;
            float fromStartVolume = from.volume;
            float toTargetVolume = targetVolume * _bgmVolume * _masterVolume;

            to.volume = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                to.volume = Mathf.Lerp(0f, toTargetVolume, t);

                yield return null;
            }

            from.Stop();
            from.volume = 0f;
            to.volume = toTargetVolume;
        }

        private IEnumerator FadeOutSource(AudioSource source, float duration)
        {
            if (duration <= 0f)
            {
                source.Stop();
                source.volume = 0f;
                yield break;
            }

            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
        }

        private IEnumerator TrackSFXEnd(AudioSource source, string id, float clipLength)
        {
            yield return new WaitForSecondsRealtime(clipLength);

            _sfxBaseVolumes.Remove(source);
            UnregisterSFXSource(source);

            if (_sfxPlayCount.TryGetValue(id, out int count))
            {
                count--;
                if (count <= 0)
                    _sfxPlayCount.Remove(id);
                else
                    _sfxPlayCount[id] = count;
            }
        }
    }
}
