using System.Numerics;

namespace Engine.Audio;

public readonly record struct AudioClipHandle(int Value);
public readonly record struct AudioVoiceHandle(int Value);
public readonly record struct AudioListener(Vector3 Position, Vector3 Forward, Vector3 Up);

public interface IAudioDevice : IDisposable
{
    AudioVoiceHandle Play(AudioClipHandle clip, Vector3 position, float volume, bool loop);
    void Stop(AudioVoiceHandle voice);
    void SetListener(AudioListener listener);
    void Update();
}
