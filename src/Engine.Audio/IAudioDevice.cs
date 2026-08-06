using System.Numerics;

namespace Engine.Audio;

public interface IAudioDevice : IDisposable
{
    AudioVoiceHandle Play(AudioClipHandle clip, Vector3 position, float volume, bool loop);
    void Stop(AudioVoiceHandle voice);
    void SetListener(AudioListener listener);
    void Update();
}
