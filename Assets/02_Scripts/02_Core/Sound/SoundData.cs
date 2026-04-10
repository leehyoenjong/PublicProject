using System;
using UnityEngine;

namespace PublicFramework
{
    public enum SoundChannel
    {
        BGM,
        SFX,
        Voice
    }

    public enum SFXPolicy
    {
        AllowMultiple,
        RejectIfPlaying,
        RestartIfPlaying,
        LimitCount
    }

    [Serializable]
    public class SoundData
    {
        [SerializeField] private string _id;
        [SerializeField] private AudioClip _clip;
        [SerializeField] private SoundChannel _channel = SoundChannel.SFX;
        [SerializeField] private SFXPolicy _policy = SFXPolicy.AllowMultiple;
        [SerializeField] private int _maxCount = 3;
        [SerializeField] private int _priority = 128;
        [SerializeField] [Range(0f, 1f)] private float _volume = 1f;
        [SerializeField] private bool _loop;

        public string Id { get => _id; set => _id = value; }
        public AudioClip Clip => _clip;
        public SoundChannel Channel => _channel;
        public SFXPolicy Policy => _policy;
        public int MaxCount => _maxCount;
        public int Priority => _priority;
        public float Volume => _volume;
        public bool Loop => _loop;
    }
}
