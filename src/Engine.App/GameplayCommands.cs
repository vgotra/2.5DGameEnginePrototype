using Engine.Ecs.Sparse;

namespace Engine.App;

internal enum GameplayCommandKind : byte
{
    AddItem,
    RemoveItem,
    Equip,
    Unequip,
    LearnSkill,
    ForgetSkill,
    Cast
}

internal struct GameplayCommand
{
    public GameplayCommandKind Kind;
    public Entity Actor;
    public Entity Target;
    public ItemId Item;
    public SkillId Skill;
    public EquipmentSlot Slot;
}
