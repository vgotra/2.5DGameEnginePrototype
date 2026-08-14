using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public sealed class VfxPool
{
    private readonly VfxSlot[] _slots;

    public VfxPool(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _slots = new VfxSlot[capacity];
    }

    public int Capacity => _slots.Length;

    public bool TryAcquire(in EffectDefinition definition, out int handle)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].Active) continue;
            _slots[i] = new VfxSlot { Active = true, Position = definition.Position, Remaining = definition.Lifetime, Texture = definition.Texture, Size = definition.SpriteSize, Color = definition.Color, Type = definition.Type };
            handle = i;
            return true;
        }
        handle = -1;
        return false;
    }

    public bool TryGet(int handle, out VfxSlot slot)
    {
        if ((uint)handle >= (uint)_slots.Length || !_slots[handle].Active) { slot = default; return false; }
        slot = _slots[handle];
        return true;
    }

    public void Update(float deltaSeconds)
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (!_slots[i].Active) continue;
            _slots[i].Remaining -= deltaSeconds;
            if (_slots[i].Remaining <= 0f) _slots[i].Active = false;
        }
    }

    public int Extract(Span<RenderItem> destination, Vector2 muzzleFlashScreenOffset = default)
    {
        int written = 0;
        for (int i = 0; i < _slots.Length && written < destination.Length; i++)
        {
            VfxSlot slot = _slots[i];
            if (!slot.Active) continue;
            float opacity = slot.Remaining <= 0f ? 0f : Math.Clamp(slot.Remaining, 0f, 1f);
            destination[written++] = new RenderItem(slot.Position, slot.Size, slot.Texture, slot.Color)
            {
                Opacity = opacity,
                Blend = BlendMode.Additive,
                AnimationFrame = (byte)(slot.Type == EffectType.SkillBurst ? 1 : 0),
                ScreenOffset = slot.Type == EffectType.MuzzleFlash ? muzzleFlashScreenOffset : default,
            };
        }
        return written;
    }

    public struct VfxSlot
    {
        public bool Active;
        public EffectType Type;
        public Vector2 Position;
        public Vector2 Size;
        public Vector4 Color;
        public TextureHandle Texture;
        public float Remaining;
    }
}
