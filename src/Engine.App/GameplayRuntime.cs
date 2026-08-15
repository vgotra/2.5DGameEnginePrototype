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

    public static CombatStats Calculate(in Attributes attributes, in Equipment equipment, in GameplayCatalog catalog, float baseHealth = 100f, float baseMana = 50f)
    {
        int equipmentAttack = 0;
        int equipmentArmor = 0;
        AddModifier(equipment.MainHand, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.OffHand, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Head, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Chest, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Hands, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Feet, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Ring1, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Ring2, in catalog, ref equipmentAttack, ref equipmentArmor);
        AddModifier(equipment.Amulet, in catalog, ref equipmentAttack, ref equipmentArmor);
        return Calculate(in attributes, equipmentAttack, equipmentArmor, baseHealth, baseMana);
    }

    private static void AddModifier(ItemId item, in GameplayCatalog catalog, ref int equipmentAttack, ref int equipmentArmor)
    {
        if (!catalog.TryGet(item, out GameplayItemDefinition definition)) return;
        AddModifier(definition.FirstModifier, ref equipmentAttack, ref equipmentArmor);
        AddModifier(definition.SecondModifier, ref equipmentAttack, ref equipmentArmor);
    }

    private static void AddModifier(StatModifier modifier, ref int equipmentAttack, ref int equipmentArmor)
    {
        if (modifier.Stat == StatId.AttackPower) equipmentAttack += modifier.Amount;
        if (modifier.Stat == StatId.Armor) equipmentArmor += modifier.Amount;
    }
}

public static class InventorySystem
{
    public static bool TryAdd(ref Inventory inventory, ItemId item, int capacity)
    {
        if (item.Value is null || capacity <= 0 || inventory.Count >= capacity) return false;
        int slot = inventory.Find(item);
        if (slot < 0) { if (inventory.Count >= Inventory.Capacity) return false; slot = inventory.Count; }
        inventory.Set(slot, item, inventory.GetQuantityBySlot(slot) + 1);
        inventory.Count = Math.Min(capacity, inventory.Count + 1);
        inventory.LastAdded = item;
        return true;
    }

    public static bool TryRemove(ref Inventory inventory, ItemId item)
    {
        int slot = inventory.Find(item);
        if (slot < 0) return false;
        int quantity = inventory.GetQuantityBySlot(slot) - 1;
        inventory.Set(slot, quantity > 0 ? item : default, Math.Max(0, quantity));
        inventory.Count = Math.Max(0, inventory.Count - 1);
        return true;
    }
}

public static class EquipmentSystem
{
    public static bool TryEquip(ref Equipment equipment, ref Inventory inventory, ItemId item, in GameplayCatalog catalog)
    {
        if (!catalog.TryGet(item, out GameplayItemDefinition definition) || definition.Slot == EquipmentSlot.Consumable || inventory.GetQuantity(item) <= 0) return false;
        ItemId previous = Get(in equipment, definition.Slot);
        Set(ref equipment, definition.Slot, item);
        InventorySystem.TryRemove(ref inventory, item);
        if (previous.Value is not null) InventorySystem.TryAdd(ref inventory, previous, Inventory.Capacity);
        return true;
    }

    public static bool TryUnequip(ref Equipment equipment, ref Inventory inventory, EquipmentSlot slot)
    {
        ItemId item = Get(in equipment, slot);
        if (item.Value is null || !InventorySystem.TryAdd(ref inventory, item, Inventory.Capacity)) return false;
        Set(ref equipment, slot, default);
        return true;
    }

    public static bool EquipMainHand(ref Equipment equipment, ItemId item) { equipment.MainHand = item; return true; }
    private static ItemId Get(in Equipment equipment, EquipmentSlot slot) => slot switch { EquipmentSlot.MainHand => equipment.MainHand, EquipmentSlot.OffHand => equipment.OffHand, EquipmentSlot.Head => equipment.Head, EquipmentSlot.Chest => equipment.Chest, EquipmentSlot.Hands => equipment.Hands, EquipmentSlot.Feet => equipment.Feet, EquipmentSlot.Ring1 => equipment.Ring1, EquipmentSlot.Ring2 => equipment.Ring2, EquipmentSlot.Amulet => equipment.Amulet, _ => default };
    private static void Set(ref Equipment equipment, EquipmentSlot slot, ItemId item) { switch (slot) { case EquipmentSlot.MainHand: equipment.MainHand = item; break; case EquipmentSlot.OffHand: equipment.OffHand = item; break; case EquipmentSlot.Head: equipment.Head = item; break; case EquipmentSlot.Chest: equipment.Chest = item; break; case EquipmentSlot.Hands: equipment.Hands = item; break; case EquipmentSlot.Feet: equipment.Feet = item; break; case EquipmentSlot.Ring1: equipment.Ring1 = item; break; case EquipmentSlot.Ring2: equipment.Ring2 = item; break; case EquipmentSlot.Amulet: equipment.Amulet = item; break; } }
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
