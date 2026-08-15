using Engine.Ecs.Sparse;

namespace Engine.App;

public static class StatSystem
{
    public static CombatStats Calculate(in Attributes attributes, int equipmentAttack, int effectArmor, float baseHealth = 100f, float baseMana = 50f)
        => new()
        {
            MaxHealth = baseHealth + attributes.Vitality * 10f,
            MaxMana = baseMana + attributes.Spirit * 8f,
            AttackPower = attributes.Strength * 2f + attributes.Dexterity + equipmentAttack,
            SpellPower = attributes.Intelligence * 2f + attributes.Spirit,
            Armor = attributes.Vitality + effectArmor,
            MoveSpeed = 2f + attributes.Dexterity * 0.1f,
            CritChance = attributes.Dexterity * 0.01f
        };
}

public static class InventorySystem
{
    public static bool TryAdd(ref Inventory inventory, ItemId item, int capacity)
    {
        if (inventory.Count >= capacity) return false;
        inventory.Count++;
        inventory.LastAdded = item;
        return true;
    }

    public static bool TryRemove(ref Inventory inventory, ItemId item)
    {
        if (inventory.Count == 0 || inventory.LastAdded != item) return false;
        inventory.Count--;
        return true;
    }
}

public static class EquipmentSystem
{
    public static bool EquipMainHand(ref Equipment equipment, ItemId item)
    {
        equipment.MainHand = item;
        return true;
    }
}

public static class CombatSystem
{
    public static bool ApplyDamage(ref Health health, int amount)
    {
        if (amount <= 0 || health.Value <= 0) return false;
        health.Value = Math.Max(0, health.Value - amount);
        return true;
    }

    public static bool TryApply(ref Health health, ref GameplayEffect effect, int damage)
    {
        if (!ApplyDamage(ref health, damage)) return false;
        effect.Stacks = Math.Max(1, effect.Stacks);
        return true;
    }

    public static void TickEffect(ref GameplayEffect effect)
    {
        if (effect.RemainingTicks > 0) effect.RemainingTicks--;
        if (effect.RemainingTicks == 0) effect.Stacks = 0;
    }

    public static void QueueDeath(EntityCommands commands, Entity entity, in Health health)
    {
        if (health.Value <= 0) commands.Destroy(entity);
    }
}
