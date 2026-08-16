using System.Numerics;
using Engine.Ecs.Sparse;

namespace Engine.App;

public enum Team : byte { Neutral, Player, Enemy }
public enum CompanionTactics : byte { Aggressive, Defensive, Support, Ranged, StayClose, ProtectPlayer, FocusPlayerTarget }
public enum StatId : byte { Strength, Dexterity, Intelligence, Vitality, Spirit, AttackPower, SpellPower, Armor, MoveSpeed, CritChance }
public enum EquipmentSlot : byte { MainHand, OffHand, Head, Chest, Hands, Feet, Ring1, Ring2, Amulet, Consumable }
public enum AiIntentKind : byte { Idle, MoveTo, Follow, Attack, Cast, Flee, Interact, Guard, Patrol }

public struct Attributes
{
    public int Strength;
    public int Dexterity;
    public int Intelligence;
    public int Vitality;
    public int Spirit;
}

public struct CombatStats
{
    public float MaxHealth;
    public float MaxMana;
    public float AttackPower;
    public float SpellPower;
    public float Armor;
    public float MoveSpeed;
    public float CritChance;
}

public struct Faction { public Team Team; }
public struct Mana { public float Current; public float Maximum; }
public struct NavigationIntent { public Vector2 Target; public bool Requested; }
public struct CharacterMovement { public Vector2 Direction; public Vector2 Destination; public Entity FollowTarget; public CharacterIntentKind Mode; }
public struct AiIntent { public AiIntentKind Kind; public Entity Target; public Vector2 Destination; public SkillId Skill; }
public struct Companion { public Entity Owner; public CompanionTactics Tactics; }
public struct QuestProgress { public QuestId Id; public int ObjectiveCount; public int CompletedCount; public bool Active; public bool Complete; }
public struct DialogueInteraction { public NpcId Npc; public bool InRange; public bool Requested; }
public struct DialogueCapability { public DialogueId Id; }
public struct MerchantCapability { public MerchantId Id; }
public struct QuestGiverCapability { public QuestId Quest; }
public struct StatModifier { public StatId Stat; public int Amount; public int DurationTicks; }
public struct GameplayEffect { public EffectId Id; public Entity Source; public int RemainingTicks; public int Stacks; }
public struct CombatEvent { public Entity Source; public Entity Target; public int Damage; public byte Kind; }

public struct SkillLoadout
{
    public const int MaxSlots = 10;
    public SkillId Slot1;
    public SkillId Slot2;
    public SkillId Slot3;
    public SkillId Slot4;
    public SkillId Slot5;
    public SkillId Slot6;
    public SkillId Slot7;
    public SkillId Slot8;
    public SkillId Slot9;
    public SkillId Slot10;
    public readonly SkillId Get(int slot) => slot is >= 0 and < MaxSlots ? slot switch { 0 => Slot1, 1 => Slot2, 2 => Slot3, 3 => Slot4, 4 => Slot5, 5 => Slot6, 6 => Slot7, 7 => Slot8, 8 => Slot9, _ => Slot10 } : default;
    public bool AssignSkill(int slot, SkillId skill, in SkillKnowledge knowledge) { if (slot is < 0 or >= MaxSlots || skill.Value is null || !knowledge.IsKnown(skill)) return false; Set(slot, skill); return true; }
    public bool RemoveSkill(int slot) { if (slot is < 0 or >= MaxSlots) return false; Set(slot, default); return true; }
    public bool RemoveSkill(SkillId skill)
    {
        for (int slot = 0; slot < MaxSlots; slot++)
            if (Get(slot) == skill) Set(slot, default);
        return true;
    }
    private void Set(int slot, SkillId skill) { switch (slot) { case 0: Slot1 = skill; break; case 1: Slot2 = skill; break; case 2: Slot3 = skill; break; case 3: Slot4 = skill; break; case 4: Slot5 = skill; break; case 5: Slot6 = skill; break; case 6: Slot7 = skill; break; case 7: Slot8 = skill; break; case 8: Slot9 = skill; break; default: Slot10 = skill; break; } }
}

public struct SkillKnowledge
{
    public const int Capacity = 10;
    private SkillId _known1, _known2, _known3, _known4, _known5, _known6, _known7, _known8, _known9, _known10;
    private byte _level1, _level2, _level3, _level4, _level5, _level6, _level7, _level8, _level9, _level10;

    public readonly bool IsKnown(SkillId skill) => Find(skill) >= 0;
    public readonly int GetLevel(SkillId skill) { int slot = Find(skill); return slot < 0 ? 0 : GetLevelBySlot(slot); }

    public bool Learn(in GameplaySkillDefinition definition, int initialLevel = 1)
    {
        if (definition.Id.Value is null || initialLevel <= 0 || initialLevel > definition.MaximumLevel || IsKnown(definition.Id)) return false;
        for (int slot = 0; slot < Capacity; slot++) if (GetSkill(slot).Value is null) { SetSkill(slot, definition.Id); SetLevel(slot, initialLevel); return true; }
        return false;
    }

    public bool Upgrade(SkillId skill, int maximumLevel)
    {
        int slot = Find(skill);
        if (slot < 0 || maximumLevel <= 0 || GetLevelBySlot(slot) >= maximumLevel) return false;
        SetLevel(slot, GetLevelBySlot(slot) + 1);
        return true;
    }

    public bool Forget(SkillId skill)
    {
        int slot = Find(skill);
        if (slot < 0) return false;
        SetSkill(slot, default);
        SetLevel(slot, 0);
        return true;
    }

    private readonly SkillId GetSkill(int slot) => slot switch { 0 => _known1, 1 => _known2, 2 => _known3, 3 => _known4, 4 => _known5, 5 => _known6, 6 => _known7, 7 => _known8, 8 => _known9, _ => _known10 };
    private readonly int Find(SkillId skill) { for (int slot = 0; slot < Capacity; slot++) if (GetSkill(slot) == skill) return slot; return -1; }
    private readonly int GetLevelBySlot(int slot) => slot switch { 0 => _level1, 1 => _level2, 2 => _level3, 3 => _level4, 4 => _level5, 5 => _level6, 6 => _level7, 7 => _level8, 8 => _level9, _ => _level10 };
    private void SetSkill(int slot, SkillId skill) { switch (slot) { case 0: _known1 = skill; break; case 1: _known2 = skill; break; case 2: _known3 = skill; break; case 3: _known4 = skill; break; case 4: _known5 = skill; break; case 5: _known6 = skill; break; case 6: _known7 = skill; break; case 7: _known8 = skill; break; case 8: _known9 = skill; break; default: _known10 = skill; break; } }
    private void SetLevel(int slot, int level) { byte value = (byte)Math.Min(byte.MaxValue, level); switch (slot) { case 0: _level1 = value; break; case 1: _level2 = value; break; case 2: _level3 = value; break; case 3: _level4 = value; break; case 4: _level5 = value; break; case 5: _level6 = value; break; case 6: _level7 = value; break; case 7: _level8 = value; break; case 8: _level9 = value; break; default: _level10 = value; break; } }
}

public struct Inventory
{
    public const int Capacity = 16;
    public int Count;
    public ItemId LastAdded;
    private ItemId _item1, _item2, _item3, _item4, _item5, _item6, _item7, _item8;
    private ItemId _item9, _item10, _item11, _item12, _item13, _item14, _item15, _item16;
    private byte _quantity1, _quantity2, _quantity3, _quantity4, _quantity5, _quantity6, _quantity7, _quantity8;
    private byte _quantity9, _quantity10, _quantity11, _quantity12, _quantity13, _quantity14, _quantity15, _quantity16;
    public readonly bool Contains(ItemId item) => Find(item) >= 0;
    public readonly int GetQuantity(ItemId item) { int slot = Find(item); return slot < 0 ? 0 : GetQuantityBySlot(slot); }
    public readonly ItemId Get(int slot) => slot is >= 0 and < Capacity ? GetItem(slot) : default;
    internal readonly int Find(ItemId item) { for (int slot = 0; slot < Capacity; slot++) if (GetItem(slot) == item) return slot; return -1; }
    internal readonly ItemId GetItem(int slot) => slot switch { 0 => _item1, 1 => _item2, 2 => _item3, 3 => _item4, 4 => _item5, 5 => _item6, 6 => _item7, 7 => _item8, 8 => _item9, 9 => _item10, 10 => _item11, 11 => _item12, 12 => _item13, 13 => _item14, 14 => _item15, _ => _item16 };
    internal readonly int GetQuantityBySlot(int slot) => slot switch { 0 => _quantity1, 1 => _quantity2, 2 => _quantity3, 3 => _quantity4, 4 => _quantity5, 5 => _quantity6, 6 => _quantity7, 7 => _quantity8, 8 => _quantity9, 9 => _quantity10, 10 => _quantity11, 11 => _quantity12, 12 => _quantity13, 13 => _quantity14, 14 => _quantity15, _ => _quantity16 };
    internal void Set(int slot, ItemId item, int quantity) { switch (slot) { case 0: _item1 = item; _quantity1 = (byte)quantity; break; case 1: _item2 = item; _quantity2 = (byte)quantity; break; case 2: _item3 = item; _quantity3 = (byte)quantity; break; case 3: _item4 = item; _quantity4 = (byte)quantity; break; case 4: _item5 = item; _quantity5 = (byte)quantity; break; case 5: _item6 = item; _quantity6 = (byte)quantity; break; case 6: _item7 = item; _quantity7 = (byte)quantity; break; case 7: _item8 = item; _quantity8 = (byte)quantity; break; case 8: _item9 = item; _quantity9 = (byte)quantity; break; case 9: _item10 = item; _quantity10 = (byte)quantity; break; case 10: _item11 = item; _quantity11 = (byte)quantity; break; case 11: _item12 = item; _quantity12 = (byte)quantity; break; case 12: _item13 = item; _quantity13 = (byte)quantity; break; case 13: _item14 = item; _quantity14 = (byte)quantity; break; case 14: _item15 = item; _quantity15 = (byte)quantity; break; default: _item16 = item; _quantity16 = (byte)quantity; break; } }
}

public struct Equipment
{
    public ItemId MainHand;
    public ItemId OffHand;
    public ItemId Head;
    public ItemId Chest;
    public ItemId Ring1;
    public ItemId Ring2;
    public ItemId Hands;
    public ItemId Feet;
    public ItemId Amulet;
}

internal struct CastRequest
{
    public SkillId Skill;
    public Entity Target;
    public bool Requested;
}
