using System.Numerics;

namespace IsometricSandbox.Game;

public sealed class ArcherGame
{
    private const int MaxArrows = 32;
    private const float ArrowSpeed = 14f;
    private const float ArrowLifetime = 1.5f;
    public const int MaxAnimals = 10;

    private readonly Random _random = new(1337);

    public TileMap Map { get; }
    public Animal[] Animals { get; }
    public Arrow[] Arrows { get; }
    public int ArrowCount { get; private set; }
    public int Score { get; private set; }
    public Vector2 PlayerStart { get; }

    public ArcherGame(TileMap map, int animalCount = MaxAnimals)
    {
        Map = map;
        animalCount = Math.Clamp(animalCount, 1, MaxAnimals);
        map.LoadLayout(MapLayout.Rows);
        PlayerStart = map.TileToWorld(MapLayout.PlayerSpawnX, MapLayout.PlayerSpawnY);
        Animals = new Animal[animalCount];
        Arrows = new Arrow[MaxArrows];
        Reset();
    }

    public void Reset()
    {
        Score = 0;
        ArrowCount = 0;
        for (int i = 0; i < Animals.Length; i++)
        {
            AnimalSpecies species = i % 2 == 0 ? AnimalSpecies.Deer : AnimalSpecies.Rabbit;
            Animals[i] = AnimalSystem.Create(species, FindSpawn());
        }
    }

    public void UpdateFixed(Vector2 player, float deltaSeconds)
    {
        for (int i = 0; i < Animals.Length; i++)
        {
            Animal animal = Animals[i];
            if (animal.Alive)
            {
                AnimalSystem.Update(ref animal, Map, player, deltaSeconds, _random);
                Animals[i] = animal;
            }
        }
        UpdateArrows(deltaSeconds);
    }

    public void Shoot(Vector2 origin, Vector2 target)
    {
        if (ArrowCount >= Arrows.Length) return;
        Vector2 direction = target - origin;
        if (direction.LengthSquared() < 0.0001f) return;
        direction = Vector2.Normalize(direction);
        Arrows[ArrowCount++] = new Arrow
        {
            Position = origin,
            Direction = direction,
            Speed = ArrowSpeed,
            Lifetime = ArrowLifetime,
        };
    }

    private void UpdateArrows(float deltaSeconds)
    {
        int writeIndex = 0;
        for (int i = 0; i < ArrowCount; i++)
        {
            Arrow arrow = Arrows[i];
            arrow.Lifetime -= deltaSeconds;
            arrow.Position += arrow.Direction * arrow.Speed * deltaSeconds;
            bool blocked = !Map.CanOccupy(arrow.Position, Arrow.Radius);
            if (arrow.Lifetime > 0f && !blocked)
            {
                bool hit = false;
                for (int j = 0; j < Animals.Length; j++)
                {
                    Animal animal = Animals[j];
                    if (!animal.Alive) continue;
                    float combined = Arrow.Radius + animal.Radius;
                    if (Vector2.DistanceSquared(animal.Position, arrow.Position) < combined * combined)
                    {
                        animal.Alive = false;
                        Animals[j] = animal;
                        hit = true;
                        Score++;
                        break;
                    }
                }
                if (!hit) { Arrows[writeIndex++] = arrow; continue; }
            }
        }
        ArrowCount = writeIndex;
    }

    private Vector2 FindSpawn()
    {
        Vector2 start = PlayerStart;
        for (int attempt = 0; attempt < 50; attempt++)
        {
            int x = _random.Next(1, Map.Width - 1);
            int y = _random.Next(1, Map.Height - 1);
            Vector2 pos = Map.TileToWorld(x, y);
            if (Map.IsWalkable(x, y) && Vector2.DistanceSquared(pos, start) > 16f) return pos;
        }
        return Map.TileToWorld(10, 10);
    }
}
