using Engine.Ecs.Sparse;

namespace Engine.App;

public static class GameplayApiVersion
{
    public const int Major = 1;
    public const int Minor = 2;
}

public abstract class Character
{
    private readonly World _world;
    private readonly Entity _entity;
    private readonly Scene _scene;

    internal Character(World world, Scene scene, Entity entity)
    {
        _world = world;
        _scene = scene;
        _entity = entity;
    }

    public bool IsAlive => _world.IsEntityAlive(_entity);

    public bool HasPendingCast => _world.HasPendingCast(_entity);

    public bool Cast(SkillId skill, Character target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (skill.Value is null || target.World != World || target.Scene != Scene || !World.Catalog.TryGet(skill, out _)) return false;
        _world.QueueCast(_entity, skill, target._entity);
        return true;
    }

    internal World World => _world;
    internal Scene Scene => _scene;
    internal Entity Entity => _entity;

    internal Entity EntityHandle => _entity;
}

public sealed class Hero : Character
{
    internal Hero(World world, Scene scene, Entity entity, HeroId id)
        : base(world, scene, entity)
    {
        Id = id;
        Inventory = new HeroInventory(world, entity);
        Equipment = new HeroEquipment(world, entity);
        Skills = new HeroSkills(world, entity);
    }

    public HeroId Id { get; }
    public HeroInventory Inventory { get; }
    public HeroEquipment Equipment { get; }
    public HeroSkills Skills { get; }
}

public sealed class Enemy : Character
{
    internal Enemy(World world, Scene scene, Entity entity, EnemyId id)
        : base(world, scene, entity)
    {
        Id = id;
    }

    public EnemyId Id { get; }
}

public sealed class Item
{
    private readonly World _world;
    private readonly Entity _entity;

    internal Item(World world, Entity entity, ItemId id)
    {
        _world = world;
        _entity = entity;
        Id = id;
    }

    public ItemId Id { get; }
    public bool IsAlive => _world.IsEntityAlive(_entity);
    internal Entity EntityHandle => _entity;
}

public sealed class Projectile
{
    private readonly World _world;
    private readonly Entity _entity;

    internal Projectile(World world, Entity entity) { _world = world; _entity = entity; }

    public bool IsAlive => _world.IsEntityAlive(_entity);
    internal Entity EntityHandle => _entity;
}

public sealed class Effect
{
    private readonly World _world;
    private readonly Entity _entity;

    internal Effect(World world, Entity entity) { _world = world; _entity = entity; }

    public bool IsAlive => _world.IsEntityAlive(_entity);
    internal Entity EntityHandle => _entity;
}

public sealed class Npc : Character
{
    internal Npc(World world, Scene scene, Entity entity, NpcId id)
        : base(world, scene, entity)
    {
        Id = id;
    }

    public NpcId Id { get; }
}

public sealed class HeroInventory
{
    private readonly World _world;
    private readonly Entity _entity;

    internal HeroInventory(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    private bool IsAlive => _world.IsEntityAlive(_entity);

    public int Count => ReadInventory().Count;

    public bool Add(ItemId item)
    {
        if (item.Value is null || !_world.Catalog.TryGet(item, out _)) return false;
        _world.QueueInventoryAdd(_entity, item);
        return true;
    }

    public bool Remove(ItemId item)
    {
        if (item.Value is null) return false;
        _world.QueueInventoryRemove(_entity, item);
        return true;
    }

    public bool Contains(ItemId item) => ReadInventory().Contains(item);

    public int GetQuantity(ItemId item) => ReadInventory().GetQuantity(item);

    private Inventory ReadInventory()
        => _world.GetInventory(_entity);
}

public sealed class HeroEquipment
{
    private readonly World _world;
    private readonly Entity _entity;

    internal HeroEquipment(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    private bool IsAlive => _world.IsEntityAlive(_entity);

    public bool Equip(EquipmentSlot slot, ItemId item)
    {
        if (item.Value is null || slot == EquipmentSlot.Consumable || !_world.Catalog.TryGet(item, out _)) return false;
        _world.QueueEquip(_entity, slot, item);
        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (slot == EquipmentSlot.Consumable) return false;
        _world.QueueUnequip(_entity, slot);
        return true;
    }

    public ItemId Get(EquipmentSlot slot)
    {
        Equipment equipment = _world.GetEquipment(_entity);
        return EquipmentSystem.GetItem(in equipment, slot);
    }
}

public sealed class HeroSkills
{
    private readonly World _world;
    private readonly Entity _entity;

    internal HeroSkills(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    private bool IsAlive => _world.IsEntityAlive(_entity);

    public bool Learn(SkillId skill)
    {
        if (skill.Value is null || !_world.Catalog.TryGet(skill, out _)) return false;
        _world.QueueLearnSkill(_entity, skill);
        return true;
    }

    public bool Forget(SkillId skill)
    {
        if (skill.Value is null) return false;
        _world.QueueForgetSkill(_entity, skill);
        return true;
    }

    public bool IsKnown(SkillId skill)
        => _world.GetSkillKnowledge(_entity).IsKnown(skill);

    public int GetLevel(SkillId skill)
        => _world.GetSkillKnowledge(_entity).GetLevel(skill);
}
