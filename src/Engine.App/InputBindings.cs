using System.Numerics;

namespace Engine.App;

public enum InputDevice : byte { KeyboardMouse, Gamepad, Virtual }
public readonly record struct ActionBinding(InputAction Action, InputDevice Device, int Code, InputAction Modifier = (InputAction)255);

public static class DefaultInputBindings
{
    public static readonly ActionBinding[] Gamepad =
    [
        new(InputAction.PrimaryAttack, InputDevice.Gamepad, 0), new(InputAction.SecondaryAttack, InputDevice.Gamepad, 1),
        new(InputAction.Interact, InputDevice.Gamepad, 2), new(InputAction.Skill1, InputDevice.Gamepad, 3),
        new(InputAction.Skill2, InputDevice.Gamepad, 4), new(InputAction.Skill3, InputDevice.Gamepad, 5),
        new(InputAction.Skill4, InputDevice.Gamepad, 6), new(InputAction.Skill5, InputDevice.Gamepad, 3, InputAction.SecondaryAttack),
        new(InputAction.Skill6, InputDevice.Gamepad, 4, InputAction.SecondaryAttack), new(InputAction.Skill7, InputDevice.Gamepad, 5, InputAction.SecondaryAttack),
        new(InputAction.Skill8, InputDevice.Gamepad, 6, InputAction.SecondaryAttack)
    ];
}

public struct VirtualInput
{
    private Vector2 _move, _aim;
    private uint _pressed, _held;
    public readonly Vector2 Move => _move;
    public readonly Vector2 Aim => _aim;
    public readonly PlayerCommand Command => new(_move, _aim, _pressed, _held);
    public void SetMove(Vector2 value) => _move = Vector2.Clamp(value, -Vector2.One, Vector2.One);
    public void SetAim(Vector2 value) => _aim = value;
    public void Press(InputAction action) { uint mask = 1u << (int)action; _pressed |= mask; _held |= mask; }
    public void SetHeld(InputAction action, bool held) { uint mask = 1u << (int)action; if (held) _held |= mask; else _held &= ~mask; }
    public void ConsumeEdges() => _pressed = 0;
}

public static class ActionSetResolver
{
    public static InputAction ResolveGamepad(int code, bool modifierHeld)
    {
        for (int i = 0; i < DefaultInputBindings.Gamepad.Length; i++)
        {
            ActionBinding binding = DefaultInputBindings.Gamepad[i];
            if (binding.Code == code && modifierHeld && binding.Modifier != (InputAction)255) return binding.Action;
        }
        for (int i = 0; i < DefaultInputBindings.Gamepad.Length; i++)
        {
            ActionBinding binding = DefaultInputBindings.Gamepad[i];
            if (binding.Code == code && binding.Modifier == (InputAction)255) return binding.Action;
        }
        return (InputAction)255;
    }
}
