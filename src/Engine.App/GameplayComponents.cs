using System.Numerics;
using Engine.Ecs.Sparse;

namespace Engine.App;

public enum Team : byte { Neutral, Player, Enemy }
public enum StatId : byte { Strength, Dexterity, Intelligence, Vitality, Spirit, AttackPower, SpellPower, Armor, MoveSpeed, CritChance }
public enum EquipmentSlot : byte { MainHand, OffHand, Head, Chest, Hands, Feet, Ring1, Ring2, Amulet }
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
public struct Companion { public Entity Owner; public byte Tactics; }
public struct QuestProgress { public QuestId Id; public int ObjectiveCount; public int CompletedCount; public bool Active; public bool Complete; }
public struct DialogueInteraction { public NpcId Npc; public bool InRange; public bool Requested; }
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
}

public struct Inventory
{
    public int Count;
    public ItemId LastAdded;
}

public struct Equipment
{
    public ItemId MainHand;
    public ItemId OffHand;
    public ItemId Head;
    public ItemId Chest;
    public ItemId Ring1;
    public ItemId Ring2;
}
