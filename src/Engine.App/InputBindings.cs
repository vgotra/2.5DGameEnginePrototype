using System.Numerics;
using Engine.Platform;

namespace Engine.App;

public enum InputDevice : byte { KeyboardMouse, Gamepad, Virtual }
public enum InputBindingKind : byte { Keyboard, Mouse }
public enum InputAxisDirection : sbyte { None, Negative, Positive }
public readonly record struct ActionBinding(InputAction Action, InputBindingKind Kind, int Code, InputAxisDirection Direction = InputAxisDirection.None, InputAction Modifier = (InputAction)255);

public sealed class InputBindingMap
{
    private const int Capacity = 64;
    private readonly ActionBinding[] _bindings = new ActionBinding[Capacity];
    private readonly ActionBinding[] _defaults;
    private int _count;

    public InputBindingMap() : this(DefaultInputBindings.KeyboardMouse) { }

    public InputBindingMap(ReadOnlySpan<ActionBinding> defaults)
    {
        _defaults = defaults.ToArray();
        ResetDefaults();
    }

    public int Count => _count;
    public ReadOnlySpan<ActionBinding> Bindings => _bindings.AsSpan(0, _count);

    public bool Add(in ActionBinding binding)
    {
        if (!IsValid(binding) || _count == Capacity || Contains(binding) || HasConflict(binding)) return false;
        _bindings[_count++] = binding;
        return true;
    }

    public bool Replace(InputAction action, in ActionBinding binding)
    {
        if (!IsValid(binding) || binding.Action != action || HasConflict(binding, action)) return false;
        Remove(action);
        return Add(binding);
    }

    public bool Remove(InputAction action)
    {
        bool removed = false;
        for (int i = _count - 1; i >= 0; i--)
        {
            if (_bindings[i].Action != action) continue;
            _bindings[i] = _bindings[--_count];
            removed = true;
        }
        return removed;
    }

    public void ResetDefaults()
    {
        _count = 0;
        for (int i = 0; i < _defaults.Length; i++) Add(_defaults[i]);
    }

    private bool HasConflict(in ActionBinding binding, InputAction ignoredAction = (InputAction)255)
    {
        for (int i = 0; i < _count; i++)
        {
            ActionBinding existing = _bindings[i];
            if (existing.Action != ignoredAction && existing.Kind == binding.Kind && existing.Code == binding.Code && existing.Direction == binding.Direction) return true;
        }
        return false;
    }

    private bool Contains(in ActionBinding binding)
    {
        for (int i = 0; i < _count; i++) if (_bindings[i] == binding) return true;
        return false;
    }

    private static bool IsValid(in ActionBinding binding)
        => (uint)binding.Action < (uint)InputAction.Count && binding.Code >= 0;
}

public static class DefaultInputBindings
{
    public static readonly ActionBinding[] KeyboardMouse =
    [
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.Up, InputAxisDirection.Negative),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.W, InputAxisDirection.Negative),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.Down, InputAxisDirection.Positive),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.S, InputAxisDirection.Positive),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.Left, InputAxisDirection.Negative),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.A, InputAxisDirection.Negative),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.Right, InputAxisDirection.Positive),
        new(InputAction.Move, InputBindingKind.Keyboard, (int)GameKey.D, InputAxisDirection.Positive),
        new(InputAction.PrimaryAttack, InputBindingKind.Mouse, (int)MouseButton.Left),
        new(InputAction.Dodge, InputBindingKind.Keyboard, (int)GameKey.Space),
        new(InputAction.Interact, InputBindingKind.Keyboard, (int)GameKey.E),
        new(InputAction.Inventory, InputBindingKind.Keyboard, (int)GameKey.I),
        new(InputAction.Skill1, InputBindingKind.Keyboard, (int)GameKey.Number1), new(InputAction.Skill2, InputBindingKind.Keyboard, (int)GameKey.Number2),
        new(InputAction.Skill3, InputBindingKind.Keyboard, (int)GameKey.Number3), new(InputAction.Skill4, InputBindingKind.Keyboard, (int)GameKey.Number4),
        new(InputAction.Skill5, InputBindingKind.Keyboard, (int)GameKey.Number5), new(InputAction.Skill6, InputBindingKind.Keyboard, (int)GameKey.Number6),
        new(InputAction.Skill7, InputBindingKind.Keyboard, (int)GameKey.Number7), new(InputAction.Skill8, InputBindingKind.Keyboard, (int)GameKey.Number8),
        new(InputAction.Skill9, InputBindingKind.Keyboard, (int)GameKey.Number9), new(InputAction.Skill10, InputBindingKind.Keyboard, (int)GameKey.Number0)
    ];

    public static readonly ActionBinding[] Gamepad =
    [
        new(InputAction.PrimaryAttack, InputBindingKind.Keyboard, 0), new(InputAction.SecondaryAttack, InputBindingKind.Keyboard, 1),
        new(InputAction.Interact, InputBindingKind.Keyboard, 2), new(InputAction.Skill1, InputBindingKind.Keyboard, 3),
        new(InputAction.Skill2, InputBindingKind.Keyboard, 4), new(InputAction.Skill3, InputBindingKind.Keyboard, 5),
        new(InputAction.Skill4, InputBindingKind.Keyboard, 6), new(InputAction.Skill5, InputBindingKind.Keyboard, 3, InputAxisDirection.None, InputAction.SecondaryAttack),
        new(InputAction.Skill6, InputBindingKind.Keyboard, 4, InputAxisDirection.None, InputAction.SecondaryAttack), new(InputAction.Skill7, InputBindingKind.Keyboard, 5, InputAxisDirection.None, InputAction.SecondaryAttack),
        new(InputAction.Skill8, InputBindingKind.Keyboard, 6, InputAxisDirection.None, InputAction.SecondaryAttack)
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
        if (modifierHeld && code >= 0 && code < 7) return (InputAction)((int)InputAction.Skill5 + code - 3);
        if (code == 3) return InputAction.Skill1;
        if (code == 4) return InputAction.Skill2;
        if (code == 5) return InputAction.Skill3;
        if (code == 6) return InputAction.Skill4;
        return (InputAction)255;
    }
}
