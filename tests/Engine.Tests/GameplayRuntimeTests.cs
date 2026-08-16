using System.Numerics;
using Engine.App;
using SparseEntity = Engine.Ecs.Sparse.Entity;

namespace Engine.Tests;

internal static class GameplayRuntimeTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Stats_IncludeAttributesAndEquipment), Stats_IncludeAttributesAndEquipment),
        new(nameof(Inventory_RespectsCapacity), Inventory_RespectsCapacity),
        new(nameof(Combat_AppliesDamageAndExpiresEffects), Combat_AppliesDamageAndExpiresEffects),
        new(nameof(Scene_TypedMarkerSpawnsRemainDeferred), Scene_TypedMarkerSpawnsRemainDeferred)
        ,new(nameof(HeroArchetypes_RegisterDeterministically), HeroArchetypes_RegisterDeterministically)
        ,new(nameof(Skills_TrackKnowledgeLevelsAndLoadout), Skills_TrackKnowledgeLevelsAndLoadout)
        ,new(nameof(Equipment_ValidatesSlotsAndModifiers), Equipment_ValidatesSlotsAndModifiers)
    ];

    private static void Stats_IncludeAttributesAndEquipment()
    {
        Attributes attributes = new() { Strength = 3, Dexterity = 4, Vitality = 5, Spirit = 2 };
        CombatStats stats = StatSystem.Calculate(in attributes, 7, 2);
        TestAssert.True(stats.MaxHealth == 150f && stats.AttackPower == 17f && stats.Armor == 7f, "derived stats include base attributes and modifiers");
    }

    private static void Inventory_RespectsCapacity()
    {
        Inventory inventory = default;
        ItemId item = new("bow");
        TestAssert.True(InventorySystem.TryAdd(ref inventory, item, 1), "first item fits");
        TestAssert.True(!InventorySystem.TryAdd(ref inventory, item, 1), "capacity blocks additional item");
        TestAssert.True(InventorySystem.TryRemove(ref inventory, item) && inventory.Count == 0, "item can be removed");
    }

    private static void Combat_AppliesDamageAndExpiresEffects()
    {
        Health health = new(10);
        GameplayEffect effect = new() { Id = new EffectId("poison"), RemainingTicks = 1 };
        TestAssert.True(CombatSystem.TryApply(ref health, ref effect, 4) && health.Value == 6, "combat applies damage");
        CombatSystem.TickEffect(ref effect);
        TestAssert.True(effect.Stacks == 0, "effect expires deterministically");
    }

    private static void Scene_TypedMarkerSpawnsRemainDeferred()
    {
        TestGame game = new();
        World world = game.Create("world");
        Scene scene = world.LoadScene("village");
        scene.Map.AddMarker("start", new System.Numerics.Vector2(2, 3));
        world.Catalog.Register(HeroIds.Rogue, new HeroDefinition(HeroType.Archer, default, 1, default, System.Numerics.Vector2.One, System.Numerics.Vector4.One));
        var entity = scene.SpawnHero(HeroIds.Rogue, "start");
        TestAssert.True(!world.EcsWorld.IsAlive(entity.EntityHandle), "typed scene spawn is deferred");
    }

    private static void HeroArchetypes_RegisterDeterministically()
    {
        TestGame game = new();
        World world = game.Create("heroes");
        world.Catalog.RegisterDefaultHeroes();
        HeroId[] ids = [HeroIds.Rogue, HeroIds.Paladin, HeroIds.Cleric, HeroIds.Druid];
        for (int i = 0; i < ids.Length; i++)
        {
            TestAssert.True(world.Catalog.TryGet(ids[i], out HeroDefinition definition), "hero archetype is registered");
            TestAssert.True(definition.Attributes.Vitality > 0 && definition.CombatStats.MaxHealth > 0f, "hero archetype has attributes and derived stats");
            SparseEntity entity = world.SpawnHero(ids[i], new MapLocation(new MapId("test"), Vector2.Zero));
            world.ApplyCommands();
            TestAssert.True(world.EcsWorld.Get<Attributes>(entity).Dexterity >= 1, "spawned hero retains attributes");
            TestAssert.True(world.EcsWorld.Get<HeroState>(entity).Type == definition.Type, "spawned hero retains archetype type");
        }
        TestAssert.True(world.Catalog.TryGet(HeroIds.Rogue, out HeroDefinition rogue) && rogue.Type == HeroType.Archer, "rogue preserves archer compatibility");
    }

    private static void Skills_TrackKnowledgeLevelsAndLoadout()
    {
        GameplayCatalog catalog = new();
        catalog.RegisterDefaultSkills();
        SkillId[] skills = [SkillIds.BasicShot, SkillIds.PowerShot, SkillIds.ShieldStrike, SkillIds.HolyStrike, SkillIds.Heal, SkillIds.Smite, SkillIds.NatureBolt, SkillIds.Root];
        SkillKnowledge knowledge = default;
        for (int index = 0; index < skills.Length; index++)
        {
            TestAssert.True(catalog.TryGet(skills[index], out GameplaySkillDefinition definition), "representative skill is cataloged");
            TestAssert.True(catalog.TryLearn(skills[index], ref knowledge) && !catalog.TryLearn(skills[index], ref knowledge), "skill learning is unique");
            TestAssert.True(knowledge.Upgrade(skills[index], definition.MaximumLevel), "known skill upgrades");
            TestAssert.True(knowledge.GetLevel(skills[index]) == 2, "skill level increments deterministically");
        }
        TestAssert.True(!catalog.TryLearn(new SkillId("unknown"), ref knowledge), "unknown skills are not learned accidentally");
        Hotbar hotbar = default;
        TestAssert.True(hotbar.AssignSkill(0, SkillIds.BasicShot, in knowledge), "known skill equips");
        TestAssert.True(hotbar.AssignSkill(0, SkillIds.PowerShot, in knowledge) && hotbar.GetSkill(0) == SkillIds.PowerShot, "slot replacement is deterministic");
        TestAssert.True(hotbar.RemoveSkill(0) && hotbar.GetSkill(0).Value is null, "slot removal clears only the loadout");
        PlayerCommand command = new(default, default, 1u << (int)InputAction.Skill1, 0);
        SkillLoadout loadout = default;
        loadout.AssignSkill(0, SkillIds.BasicShot, in knowledge);
        CharacterIntent intent = PlayerIntentMapper.FromCommand(in command, in loadout, in knowledge);
        TestAssert.True(intent.Kind == CharacterIntentKind.Cast && intent.Skill == SkillIds.BasicShot, "learned equipped skill creates cast intent");
    }

    private static void Equipment_ValidatesSlotsAndModifiers()
    {
        GameplayCatalog catalog = new();
        catalog.RegisterDefaultItems();
        Inventory inventory = default;
        ItemId bow = GameContent.GoblinSlayerBow;
        TestAssert.True(InventorySystem.TryAdd(ref inventory, bow, Inventory.Capacity), "catalog item enters inventory");
        TestAssert.True(inventory.Contains(bow) && inventory.GetQuantity(bow) == 1, "inventory tracks item quantity");
        Equipment equipment = default;
        TestAssert.True(EquipmentSystem.TryEquip(ref equipment, ref inventory, bow, in catalog), "compatible item equips");
        TestAssert.True(equipment.MainHand == bow && !inventory.Contains(bow), "equipped item leaves inventory");
        Attributes attributes = new() { Strength = 1, Dexterity = 1, Vitality = 1, Spirit = 1 };
        CombatStats stats = StatSystem.Calculate(in attributes, in equipment, in catalog);
        TestAssert.True(stats.AttackPower == 10f, "equipment attack modifier affects derived stats");
        TestAssert.True(!EquipmentSystem.TryEquip(ref equipment, ref inventory, new ItemId("unknown"), in catalog), "unknown item is rejected");
        TestAssert.True(EquipmentSystem.TryUnequip(ref equipment, ref inventory, EquipmentSlot.MainHand) && inventory.Contains(bow), "unequip returns item to inventory");
    }

    private sealed class TestGame : Game
    {
        public World Create(string name) => CreateWorld(name);
    }
}
