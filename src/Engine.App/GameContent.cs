using System.Numerics;
using Engine.Rendering;

namespace Engine.App;

public static class GameContent
{
    public static readonly SceneId VillageScene = new("village");
    public static readonly SceneId GoblinForestScene = new("goblin-forest");
    public static readonly MapId VillageMap = new("village-map");
    public static readonly MapId GoblinForestMap = new("goblin-forest-map");
    public static readonly QuestId GoblinProblem = new("goblin-problem");
    public static readonly EnemyId GoblinWarrior = new("goblin-warrior");
    public static readonly EnemyId GoblinArcher = new("goblin-archer");
    public static readonly EnemyId GoblinShaman = new("goblin-shaman");
    public static readonly NpcId ElderMarcus = new("elder-marcus");
    public static readonly NpcId VillageBlacksmith = new("village-blacksmith");
    public static readonly NpcId ClericCompanion = new("cleric-companion");
    public static readonly ItemId GoblinSlayerBow = new("goblin-slayer-bow");
    public static readonly EffectId Poison = new("poison");
    public static readonly EffectId Burning = new("burning");

    public static QuestDefinition CreateGoblinProblem()
        => new(GoblinProblem,
            new QuestObjectiveDefinition(QuestObjectiveType.KillEnemy, GoblinWarrior, default, 3),
            new QuestObjectiveDefinition(QuestObjectiveType.ReturnToQuestGiver, default, ElderMarcus, 1),
            new QuestReward(GoblinSlayerBow, 100));

    public static void ConfigureWorld(World world)
    {
        world.Map.Register(new WorldLocation(new WorldMapLocationId("village"), VillageScene));
        world.Map.Register(new WorldLocation(new WorldMapLocationId("goblin-forest"), GoblinForestScene));
        world.Map.Connect(new WorldMapLocationId("village"), new WorldMapLocationId("goblin-forest"));
        world.Map.Unlock(new WorldMapLocationId("village"));
    }

    public static void ConfigureWorld(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        WorldMap map = game.WorldMap ?? throw new InvalidOperationException("The game runtime world has not been created.");
        map.Register(new WorldLocation(new WorldMapLocationId("village"), VillageScene));
        map.Register(new WorldLocation(new WorldMapLocationId("goblin-forest"), GoblinForestScene));
        map.Connect(new WorldMapLocationId("village"), new WorldMapLocationId("goblin-forest"));
        map.Unlock(new WorldMapLocationId("village"));
    }

    public static void ConfigureVillage(Scene scene)
    {
        scene.Map.AddMarker("player-start", new Vector2(3, 3));
        scene.Map.AddMarker("elder-marcus", new Vector2(7, 5));
        scene.Map.AddMarker("blacksmith", new Vector2(10, 5));
        scene.Map.AddMarker("forest-exit", new Vector2(16, 10));
    }

    public static void ConfigureGoblinForest(Scene scene)
    {
        scene.Map.AddMarker("warrior-camp", new Vector2(6, 8));
        scene.Map.AddMarker("archer-camp", new Vector2(10, 9));
        scene.Map.AddMarker("shaman-camp", new Vector2(14, 12));
        scene.Map.AddMarker("cleric-companion", new Vector2(4, 4));
        scene.Map.AddMarker("loot", new Vector2(15, 14));
        scene.Map.AddMarker("boss-arena", new Vector2(18, 18));
        scene.Map.AddMarker("village-return", new Vector2(2, 2));
    }
}
