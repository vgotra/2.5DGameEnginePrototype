using System.Numerics;
using Engine.Core;
using Engine.Ecs;
using Engine.Mathematics;
using IsometricSandbox.Game;
using Engine.Rendering;

Vector2 original = new(7, 2);
Vector2 projected = IsometricMath.WorldToScreen(original, 64, 32);
Vector2 restored = IsometricMath.ScreenToWorld(projected, 64, 32);
Assert(MathF.Abs(restored.X - original.X) < 0.001f && MathF.Abs(restored.Y - original.Y) < 0.001f, "isometric conversion");

World world = new();
EntityId entity = world.Create();
world.Storage<TestComponent>().Add(entity, new TestComponent(42));
Assert(world.IsAlive(entity), "entity alive");
Assert(world.Storage<TestComponent>().TryGet(entity, out TestComponent value) && value.Value == 42, "component storage");
world.Destroy(entity);
Assert(!world.IsAlive(entity), "stale entity rejected");
TileMap map = new();
Assert(map.IsInside(1, 1) && !map.IsInside(-1, 1), "tile bounds");
Assert(map.IsWalkable(0, 0), "open map");
Vector2 moved = MovementSystem.Move(map, new Vector2(2.5f, 2.5f), new Vector2(1, 1), 1, 0.2f, 1);
Assert(moved.X > 2.5f && moved.Y > 2.5f, "free movement");
Assert(map.CanOccupy(new Vector2(10.5f, 10.5f), 0.2f), "open movement");
IsometricCamera camera = new(new Vector2(800, 600));
camera.Follow(new Vector2(10, 10), map);
Assert(camera.WorldToScreen(new Vector2(10, 10), map) == new Vector2(400, 300), "camera center");
ShapeVertex[] geometry = new ShapeVertex[6];
Assert(GeneratedGeometry.AppendDiamond(geometry, Vector2.Zero, 64, 32, Vector4.One) == 6, "diamond geometry");
Assert(RenderExtractionSystem.ExtractMap(map, camera, new ShapeVertex[map.Width * map.Height * 6]) > 0, "map extraction");
Console.WriteLine("Smoke tests passed");

static void Assert(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException($"Failed: {name}");
}

public readonly record struct TestComponent(int Value);
