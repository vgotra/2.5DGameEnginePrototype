namespace Engine.App;

public struct CharacterVisual
{
    public int AssetId;
    public int ClipId;
    public float AnimationTime;
    public byte AnimationFrame;
    public byte Direction;
    public float Opacity;
    public float Scale;
}

public static class CharacterAnimationSystem
{
    public static void Tick(ref CharacterVisual visual, float fixedStep, int framesPerSecond, int frameCount, int directionCount)
    {
        if (framesPerSecond <= 0 || frameCount <= 0) return;
        visual.AnimationTime += fixedStep;
        visual.AnimationFrame = (byte)((int)(visual.AnimationTime * framesPerSecond) % frameCount);
        if (directionCount > 0) visual.Direction = (byte)(visual.Direction % directionCount);
    }
}
