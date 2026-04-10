namespace PublicFramework
{
    public interface ISoundManager : IService
    {
        void PlayBGM(string id, float fadeTime = 1f);
        void StopBGM(float fadeTime = 1f);
        void PlaySFX(string id);
        void StopSFX(string id);
        void PlayVoice(string id);
        void StopVoice();
        void StopAll();

        void SetMasterVolume(float volume);
        void SetBGMVolume(float volume);
        void SetSFXVolume(float volume);
        void SetVoiceVolume(float volume);

        float MasterVolume { get; }
        float BGMVolume { get; }
        float SFXVolume { get; }
        float VoiceVolume { get; }

        void SetMute(SoundChannel channel, bool mute);
        bool IsMuted(SoundChannel channel);

        void RegisterSound(SoundData data);
    }
}
