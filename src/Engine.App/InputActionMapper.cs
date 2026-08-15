using System.Numerics;
using Engine.Platform;

namespace Engine.App;

public static class InputActionMapper
{
    public static void Sample(IInputState input, ref InputActionBuffer buffer)
    {
        input.Update();
        CaptureCurrent(input, ref buffer);
    }

    public static void CaptureCurrent(IInputState input, ref InputActionBuffer buffer)
    {
        buffer.Move = new Vector2(
            Axis(input.IsDown(GameKey.Right), input.IsDown(GameKey.Left)),
            Axis(input.IsDown(GameKey.Down), input.IsDown(GameKey.Up)));
        buffer.Aim = input.MousePosition;
        buffer.Held = 0;
        buffer.Pressed = 0;
        Set(ref buffer, InputAction.PrimaryAttack, input.IsMouseDown, input.MousePressed);
        Set(ref buffer, InputAction.Dodge, input.IsDown(GameKey.Space), input.WasPressed(GameKey.Space));
        Set(ref buffer, InputAction.Interact, input.IsDown(GameKey.E), input.WasPressed(GameKey.E));
        Set(ref buffer, InputAction.Inventory, input.IsDown(GameKey.I), input.WasPressed(GameKey.I));
        SetSkill(ref buffer, input, InputAction.Skill1, GameKey.Number1);
        SetSkill(ref buffer, input, InputAction.Skill2, GameKey.Number2);
        SetSkill(ref buffer, input, InputAction.Skill3, GameKey.Number3);
        SetSkill(ref buffer, input, InputAction.Skill4, GameKey.Number4);
        SetSkill(ref buffer, input, InputAction.Skill5, GameKey.Number5);
        SetSkill(ref buffer, input, InputAction.Skill6, GameKey.Number6);
        SetSkill(ref buffer, input, InputAction.Skill7, GameKey.Number7);
        SetSkill(ref buffer, input, InputAction.Skill8, GameKey.Number8);
        SetSkill(ref buffer, input, InputAction.Skill9, GameKey.Number9);
        SetSkill(ref buffer, input, InputAction.Skill10, GameKey.Number0);
    }

    private static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);

    private static void Set(ref InputActionBuffer buffer, InputAction action, bool held, bool pressed)
    {
        uint mask = 1u << (int)action;
        if (held) buffer.Held |= mask;
        if (pressed) buffer.Pressed |= mask;
    }

    private static void SetSkill(ref InputActionBuffer buffer, IInputState input, InputAction action, GameKey key)
        => Set(ref buffer, action, input.IsDown(key), input.WasPressed(key));
}
