using System.Numerics;

namespace Engine.Audio;

public readonly record struct AudioListener(Vector3 Position, Vector3 Forward, Vector3 Up);
