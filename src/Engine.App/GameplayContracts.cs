using System.Numerics;
using Engine.Ecs.Sparse;

namespace Engine.App;

public enum EffectStackPolicy : byte { Refresh, Stack, Replace }
public enum QuestObjectiveType : byte { KillEnemy, TalkToNpc, ReturnToQuestGiver }
public enum NpcCapability : byte { Dialogue, Merchant, QuestGiver, Companion, Combatant }

public readonly record struct GameplayEffectDefinition(
    EffectId Id,
    int DurationTicks,
    int TickInterval,
    int Value,
    StatId ModifiedStat,
    int StatAmount,
    EffectStackPolicy StackPolicy,
    VfxId Vfx,
    SoundId Sound);

public readonly record struct QuestObjectiveDefinition(QuestObjectiveType Type, EnemyId Enemy, NpcId Npc, int Required);
public readonly record struct QuestReward(ItemId Item, int Gold);
public readonly record struct QuestDefinition(QuestId Id, QuestObjectiveDefinition First, QuestObjectiveDefinition Second, QuestReward Reward);
public readonly record struct NpcCapabilities(NpcCapability Flags);
public readonly record struct AiDefinition(AiIntentKind DefaultIntent, float AggroRange, float FleeHealthPercent, SkillId SupportSkill);
public readonly record struct PresentationReaction(byte Kind, VfxId Vfx, SoundId Sound, Entity Source, Entity Target, int Value);

public struct ActiveEffects
{
    public GameplayEffect First;
    public GameplayEffect Second;
    public GameplayEffect Third;
    public GameplayEffect Fourth;
}

public struct SkillState
{
    public SkillId Known1;
    public SkillId Known2;
    public SkillId Equipped1;
    public SkillId Equipped2;
    public int Level1;
    public int Level2;
    public float Cooldown1;
    public float Cooldown2;
}

public struct QuestState
{
    public QuestId Id;
    public int FirstProgress;
    public int SecondProgress;
    public bool Active;
    public bool Complete;
    public bool RewardClaimed;
}

public struct AiState
{
    public AiIntent Intent;
    public AiDefinition Definition;
    public int CooldownTicks;
}

public static class EffectRuntime
{
    public static bool Apply(ref ActiveEffects effects, in GameplayEffectDefinition definition, Entity source)
    {
        ref GameplayEffect slot = ref FindSlot(ref effects, definition.Id);
        if (slot.Id == definition.Id)
        {
            if (definition.StackPolicy == EffectStackPolicy.Stack) slot.Stacks++;
            if (definition.StackPolicy != EffectStackPolicy.Replace) slot.RemainingTicks = definition.DurationTicks;
            if (definition.StackPolicy == EffectStackPolicy.Replace) slot = new GameplayEffect { Id = definition.Id, Source = source, RemainingTicks = definition.DurationTicks, Stacks = 1 };
            return true;
        }

        if (slot.Id.Value is not null) return false;
        slot = new GameplayEffect { Id = definition.Id, Source = source, RemainingTicks = definition.DurationTicks, Stacks = 1 };
        return true;
    }

    public static void Tick(ref ActiveEffects effects)
    {
        Tick(ref effects.First);
        Tick(ref effects.Second);
        Tick(ref effects.Third);
        Tick(ref effects.Fourth);
    }

    private static void Tick(ref GameplayEffect effect)
    {
        if (effect.Id.Value is null) return;
        if (effect.RemainingTicks > 0) effect.RemainingTicks--;
        if (effect.RemainingTicks == 0) effect = default;
    }

    private static ref GameplayEffect FindSlot(ref ActiveEffects effects, EffectId id)
    {
        if (effects.First.Id == id || effects.First.Id.Value is null) return ref effects.First;
        if (effects.Second.Id == id || effects.Second.Id.Value is null) return ref effects.Second;
        if (effects.Third.Id == id || effects.Third.Id.Value is null) return ref effects.Third;
        return ref effects.Fourth;
    }
}

public static class QuestRuntime
{
    public static bool Activate(ref QuestState state, in QuestDefinition definition)
    {
        if (state.Active || state.Complete) return false;
        state = new QuestState { Id = definition.Id, Active = true };
        return true;
    }

    public static bool RecordKill(ref QuestState state, EnemyId enemy, in QuestDefinition definition)
    {
        if (!state.Active || state.Id != definition.Id || definition.First.Type != QuestObjectiveType.KillEnemy || definition.First.Enemy != enemy) return false;
        state.FirstProgress = Math.Min(definition.First.Required, state.FirstProgress + 1);
        return true;
    }

    public static bool CompleteReturn(ref QuestState state, NpcId npc, in QuestDefinition definition)
    {
        if (!state.Active || state.Id != definition.Id || state.FirstProgress < definition.First.Required || definition.Second.Npc != npc) return false;
        state.Complete = true;
        state.Active = false;
        return true;
    }
}

public static class AiRuntime
{
    public static void Evaluate(ref AiState state, Vector2 position, Vector2 ownerPosition, float healthPercent, Entity owner, Entity target)
    {
        if (healthPercent <= state.Definition.FleeHealthPercent) state.Intent = new AiIntent { Kind = AiIntentKind.Flee, Target = target };
        else if (state.Definition.SupportSkill.Value is not null && healthPercent < 0.5f) state.Intent = new AiIntent { Kind = AiIntentKind.Cast, Skill = state.Definition.SupportSkill, Target = owner };
        else if (target.IsValid) state.Intent = new AiIntent { Kind = AiIntentKind.Attack, Target = target };
        else if (Vector2.DistanceSquared(position, ownerPosition) > 25f) state.Intent = new AiIntent { Kind = AiIntentKind.Follow, Target = owner, Destination = ownerPosition };
        else state.Intent = new AiIntent { Kind = state.Definition.DefaultIntent };
    }
}
