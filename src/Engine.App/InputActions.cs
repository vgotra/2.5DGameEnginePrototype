using System.Numerics;

namespace Engine.App;

public enum InputAction : byte
{
    Move,
    Aim,
    PrimaryAttack,
    SecondaryAttack,
    Interact,
    Pickup,
    Inventory,
    Character,
    WorldMap,
    Dodge,
    UsePotion,
    Skill1,
    Skill2,
    Skill3,
    Skill4,
    Skill5,
    Skill6,
    Skill7,
    Skill8,
    Skill9,
    Skill10
}

public readonly record struct PlayerCommand(Vector2 Move, Vector2 Aim, uint Pressed, uint Held)
{
    public bool IsPressed(InputAction action) => (Pressed & Mask(action)) != 0;
    public bool IsHeld(InputAction action) => (Held & Mask(action)) != 0;

    private static uint Mask(InputAction action) => 1u << (int)action;
}

public struct InputActionBuffer
{
    public Vector2 Move;
    public Vector2 Aim;
    public uint Pressed;
    public uint Held;

    public readonly PlayerCommand Snapshot() => new(Move, Aim, Pressed, Held);

    public void ClearEdges() => Pressed = 0;
}
