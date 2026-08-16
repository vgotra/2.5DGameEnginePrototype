using System.Numerics;
using Engine.Platform;

namespace Engine.App;

public static class InputActionMapper
{
    private static readonly InputBindingMap DefaultMap = new();

    public static void Sample(IInputState input, ref InputActionBuffer buffer, InputBindingMap bindings)
    {
        input.Update();
        CaptureCurrent(input, ref buffer, bindings);
    }

    public static void Sample(IInputState input, ref InputActionBuffer buffer)
    {
        input.Update();
        CaptureCurrent(input, ref buffer);
    }

    public static void CaptureCurrent(IInputState input, ref InputActionBuffer buffer)
        => CaptureCurrent(input, ref buffer, DefaultMap);

    public static void CaptureCurrent(IInputState input, ref InputActionBuffer buffer, InputBindingMap bindings)
    {
        float moveX = 0f, moveY = 0f;
        buffer.Aim = input.MousePosition;
        buffer.Held = 0;
        buffer.Pressed = 0;
        ReadOnlySpan<ActionBinding> map = bindings.Bindings;
        for (int i = 0; i < map.Length; i++)
        {
            ActionBinding binding = map[i];
            bool held = binding.Kind == InputBindingKind.Keyboard ? input.IsDown((GameKey)binding.Code) : input.IsMouseButtonDown((MouseButton)binding.Code);
            bool pressed = binding.Kind == InputBindingKind.Keyboard ? input.WasPressed((GameKey)binding.Code) : input.WasMouseButtonPressed((MouseButton)binding.Code);
            if (binding.Action == InputAction.Move)
            {
                if (held && (binding.Code == (int)GameKey.Left || binding.Code == (int)GameKey.A)) moveX -= 1f;
                if (held && (binding.Code == (int)GameKey.Right || binding.Code == (int)GameKey.D)) moveX += 1f;
                if (held && (binding.Code == (int)GameKey.Up || binding.Code == (int)GameKey.W)) moveY -= 1f;
                if (held && (binding.Code == (int)GameKey.Down || binding.Code == (int)GameKey.S)) moveY += 1f;
                continue;
            }
            Set(ref buffer, binding.Action, held, pressed);
        }
        buffer.Move = Vector2.Clamp(new Vector2(moveX, moveY), -Vector2.One, Vector2.One);
    }

    public static void CaptureCurrent(IGameInput input, ref InputActionBuffer buffer, InputBindingMap bindings)
    {
        float moveX = 0f, moveY = 0f;
        buffer.Aim = input.MousePosition;
        buffer.Held = 0;
        buffer.Pressed = 0;
        ReadOnlySpan<ActionBinding> map = bindings.Bindings;
        for (int index = 0; index < map.Length; index++)
        {
            ActionBinding binding = map[index];
            bool held = binding.Kind == InputBindingKind.Keyboard
                ? input.IsDown((GameKey)binding.Code)
                : input.IsMouseButtonDown((MouseButton)binding.Code);
            bool pressed = binding.Kind == InputBindingKind.Keyboard
                ? input.WasPressed((GameKey)binding.Code)
                : input.WasMouseButtonPressed((MouseButton)binding.Code);
            if (binding.Action == InputAction.Move)
            {
                if (held && (binding.Code == (int)GameKey.Left || binding.Code == (int)GameKey.A)) moveX -= 1f;
                if (held && (binding.Code == (int)GameKey.Right || binding.Code == (int)GameKey.D)) moveX += 1f;
                if (held && (binding.Code == (int)GameKey.Up || binding.Code == (int)GameKey.W)) moveY -= 1f;
                if (held && (binding.Code == (int)GameKey.Down || binding.Code == (int)GameKey.S)) moveY += 1f;
                continue;
            }
            Set(ref buffer, binding.Action, held, pressed);
        }
        buffer.Move = Vector2.Clamp(new Vector2(moveX, moveY), -Vector2.One, Vector2.One);
    }

    private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

    private static void Set(ref InputActionBuffer buffer, InputAction action, bool held, bool pressed)
    {
        uint mask = 1u << (int)action;
        if (held) buffer.Held |= mask;
        if (pressed) buffer.Pressed |= mask;
    }

}
