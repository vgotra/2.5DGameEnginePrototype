using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public struct Renderable(TextureHandle texture, Vector2 size, Vector4 color)
{
    public TextureHandle Texture = texture;
    public Vector2 Size = size;
    public Vector4 Color = color;
    public Vector4 BottomColor = color;
    public float Scale = 1f;
    public float Opacity = 1f;
    public byte AnimationFrame;
    public BlendMode Blend = BlendMode.Alpha;
    public MaterialHandle Material;
    public CharacterVisual Character;
    public Vector2 UvScale = Vector2.One;
    public Vector2 UvOffset;

    public readonly RenderItem ToRenderItem(Vector2 worldPosition)
        => new(worldPosition, Size, Texture, new Vector4(Color.X, Color.Y, Color.Z, Color.W * Opacity), SortKey: 0f)
        {
            BottomColor = BottomColor,
            Material = Material,
            Scale = Scale,
            Opacity = Opacity,
            AnimationFrame = AnimationFrame,
            Blend = Blend,
            UvScale = UvScale,
            UvOffset = UvOffset,
        };

    public void ApplyCookedFrame(in GltfCookedCharacter asset)
    {
        Texture = asset.Atlas;
        int frame = Character.AnimationFrame + Character.Direction * Math.Max(1, asset.FrameCount / Math.Max(1, asset.Directions));
        if ((uint)frame >= (uint)asset.UvOffsets.Length) return;
        UvOffset = asset.UvOffsets[frame];
        UvScale = asset.UvScales[frame];
        AnimationFrame = (byte)Math.Clamp(frame, 0, byte.MaxValue);
    }

    public void AdvanceCharacter(float fixedStep, int framesPerSecond, int frameCount, int directionCount)
    {
        CharacterAnimationSystem.Tick(ref Character, fixedStep, framesPerSecond, frameCount, directionCount);
        AnimationFrame = Character.AnimationFrame;
        Scale = Character.Scale > 0f ? Character.Scale : Scale;
        Opacity = Character.Opacity > 0f ? Character.Opacity : Opacity;
    }
}
