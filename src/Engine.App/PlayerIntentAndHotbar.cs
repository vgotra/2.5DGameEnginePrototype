using System.Numerics;
using Engine.Ecs.Sparse;

namespace Engine.App;

public enum CharacterIntentKind : byte { Stop, Move, MoveTo, Follow, Attack, Cast, Interact, UseItem }
public readonly record struct CharacterIntent(CharacterIntentKind Kind, Vector2 Direction, Vector2 Destination, Entity Target, SkillId Skill, ItemId Item);
public readonly record struct MovementRequest(CharacterIntentKind Kind, Vector2 Direction, Vector2 Destination, Entity FollowTarget);
public enum NavigationResult : byte { None, Moving, Arrived, Blocked }

public static class PlayerIntentMapper
{
    public static CharacterIntent FromCommand(in PlayerCommand command, SkillLoadout loadout)
    {
        if (command.Move.LengthSquared() > 0.0001f) return new CharacterIntent(CharacterIntentKind.Move, Vector2.Normalize(command.Move), default, default, default, default);
        if (command.IsPressed(InputAction.Interact)) return new CharacterIntent(CharacterIntentKind.Interact, default, default, default, default, default);
        for (int slot = 0; slot < SkillLoadout.MaxSlots; slot++)
        {
            InputAction action = (InputAction)((int)InputAction.Skill1 + slot);
            if (command.IsPressed(action)) return new CharacterIntent(CharacterIntentKind.Cast, default, default, default, loadout.Get(slot), default);
        }
        return new CharacterIntent(CharacterIntentKind.Stop, default, default, default, default, default);
    }

    public static CharacterIntent FromCommand(in PlayerCommand command, in SkillLoadout loadout, in SkillKnowledge knowledge)
    {
        if (command.Move.LengthSquared() > 0.0001f) return new CharacterIntent(CharacterIntentKind.Move, Vector2.Normalize(command.Move), default, default, default, default);
        if (command.IsPressed(InputAction.Interact)) return new CharacterIntent(CharacterIntentKind.Interact, default, default, default, default, default);
        for (int i = 0; i < SkillLoadout.MaxSlots; i++)
        {
            InputAction action = (InputAction)((int)InputAction.Skill1 + i);
            SkillId skill = loadout.Get(i);
            if (command.IsPressed(action) && skill.Value is not null && knowledge.IsKnown(skill)) return new CharacterIntent(CharacterIntentKind.Cast, default, default, default, skill, default);
        }
        return new CharacterIntent(CharacterIntentKind.Stop, default, default, default, default, default);
    }
}

public struct Hotbar
{
    public const int Capacity = 10;
    private SkillId _skill1, _skill2, _skill3, _skill4, _skill5, _skill6, _skill7, _skill8, _skill9, _skill10;
    private ItemId _item1, _item2, _item3, _item4, _item5, _item6, _item7, _item8, _item9, _item10;

    public readonly SkillId GetSkill(int slot) => slot is >= 0 and < Capacity ? slot switch { 0 => _skill1, 1 => _skill2, 2 => _skill3, 3 => _skill4, 4 => _skill5, 5 => _skill6, 6 => _skill7, 7 => _skill8, 8 => _skill9, _ => _skill10 } : default;
    public readonly ItemId GetItem(int slot) => slot is >= 0 and < Capacity ? slot switch { 0 => _item1, 1 => _item2, 2 => _item3, 3 => _item4, 4 => _item5, 5 => _item6, 6 => _item7, 7 => _item8, 8 => _item9, _ => _item10 } : default;
    public bool AssignSkill(int slot, SkillId skill) { if (slot is < 0 or >= Capacity) return false; SetSkill(slot, skill); return true; }
    public bool AssignSkill(int slot, SkillId skill, in SkillKnowledge knowledge) => slot is >= 0 and < Capacity && skill.Value is not null && knowledge.IsKnown(skill) && SetKnownSkill(slot, skill);
    public bool RemoveSkill(int slot) { if (slot is < 0 or >= Capacity) return false; SetSkill(slot, default); return true; }
    public bool AssignItem(int slot, ItemId item) { if (slot is < 0 or >= Capacity) return false; SetItem(slot, item); return true; }

    private void SetSkill(int slot, SkillId value) { switch (slot) { case 0: _skill1 = value; break; case 1: _skill2 = value; break; case 2: _skill3 = value; break; case 3: _skill4 = value; break; case 4: _skill5 = value; break; case 5: _skill6 = value; break; case 6: _skill7 = value; break; case 7: _skill8 = value; break; case 8: _skill9 = value; break; default: _skill10 = value; break; } }
    private bool SetKnownSkill(int slot, SkillId value) { SetSkill(slot, value); return true; }
    private void SetItem(int slot, ItemId value) { switch (slot) { case 0: _item1 = value; break; case 1: _item2 = value; break; case 2: _item3 = value; break; case 3: _item4 = value; break; case 4: _item5 = value; break; case 5: _item6 = value; break; case 6: _item7 = value; break; case 7: _item8 = value; break; case 8: _item9 = value; break; default: _item10 = value; break; } }
}

public static class Navigation
{
    private static readonly Point[] Neighbors = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];
    public static int BuildDirectPath(Vector2 start, Vector2 destination, Span<Vector2> path)
    {
        int count = 0; int x = (int)MathF.Round(start.X), y = (int)MathF.Round(start.Y); int tx = (int)MathF.Round(destination.X), ty = (int)MathF.Round(destination.Y);
        if (count < path.Length) path[count++] = new Vector2(x, y);
        while ((x != tx || y != ty) && count < path.Length) { if (x != tx) x += Math.Sign(tx - x); else y += Math.Sign(ty - y); path[count++] = new Vector2(x, y); }
        return count;
    }

    public static int BuildGridPath(TerrainSurface terrain, Vector2 start, Vector2 destination, float radius, Span<Vector2> path)
    {
        int width = terrain.Width, height = terrain.Height, total = width * height;
        int startX = Math.Clamp((int)MathF.Floor(start.X), 0, width - 1), startY = Math.Clamp((int)MathF.Floor(start.Y), 0, height - 1);
        int targetX = Math.Clamp((int)MathF.Floor(destination.X), 0, width - 1), targetY = Math.Clamp((int)MathF.Floor(destination.Y), 0, height - 1);
        int startIndex = startY * width + startX, targetIndex = targetY * width + targetX;
        int[] queue = new int[total], parents = new int[total]; bool[] visited = new bool[total];
        Array.Fill(parents, -1); int head = 0, tail = 0; queue[tail++] = startIndex; visited[startIndex] = true;
        while (head < tail)
        {
            int current = queue[head++]; if (current == targetIndex) break;
            int x = current % width, y = current / width;
            for (int i = 0; i < Neighbors.Length; i++)
            {
                int nx = x + Neighbors[i].X, ny = y + Neighbors[i].Y; if ((uint)nx >= (uint)width || (uint)ny >= (uint)height) continue;
                int next = ny * width + nx; if (visited[next] || !terrain.CanOccupy(terrain.TileToWorld(nx, ny), radius)) continue;
                visited[next] = true; parents[next] = current; queue[tail++] = next;
            }
        }
        if (!visited[targetIndex]) return 0;
        int length = 0, cursor = targetIndex; while (cursor >= 0 && length < path.Length) { path[length++] = terrain.TileToWorld(cursor % width, cursor / width); cursor = parents[cursor]; }
        for (int i = 0, j = length - 1; i < j; i++, j--) (path[i], path[j]) = (path[j], path[i]);
        return length;
    }
    private readonly record struct Point(int X, int Y);
}

public static class NavigationRuntime
{
    public static void Apply(ref CharacterMovement movement, in CharacterIntent intent)
    {
        movement.Mode = intent.Kind;
        movement.Direction = intent.Direction;
        movement.Destination = intent.Destination;
        movement.FollowTarget = intent.Target;
    }

    public static Vector2 Step(Vector2 position, in CharacterMovement movement, float speed, float fixedStep)
    {
        Vector2 direction = movement.Mode == CharacterIntentKind.Move ? movement.Direction : movement.Destination - position;
        if (direction.LengthSquared() < 0.0001f) return position;
        direction = Vector2.Normalize(direction);
        float distance = speed * fixedStep;
        if (movement.Mode != CharacterIntentKind.Move && (movement.Destination - position).LengthSquared() <= distance * distance) return movement.Destination;
        return position + direction * distance;
    }
}
