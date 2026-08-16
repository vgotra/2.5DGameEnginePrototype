using Engine.Ecs.Sparse;

namespace Engine.App;

public static class GameplayApiVersion
{
    public const int Major = 1;
    public const int Minor = 0;
}

public abstract class Character
{
    private readonly World _world;
    private readonly Entity _entity;

    internal Character(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public bool IsAlive => _world.IsEntityAlive(_entity);

    public bool HasPendingCast => _world.HasPendingCast(_entity);

    public bool Cast(SkillId skill, Character target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (skill.Value is null || target.World != World || !World.Catalog.TryGet(skill, out _)) return false;
        _world.QueueCast(_entity, skill, target._entity);
        return true;
    }

    internal World World => _world;
    internal Entity Entity => _entity;

    internal Entity EntityHandle => _entity;
}

public sealed class Hero : Character
{
    internal Hero(World world, Entity entity, HeroId id)
        : base(world, entity)
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
    internal Enemy(World world, Entity entity, EnemyId id)
        : base(world, entity)
    {
        Id = id;
    }

    public EnemyId Id { get; }
}

public sealed class Npc : Character
{
    internal Npc(World world, Entity entity, NpcId id)
        : base(world, entity)
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
