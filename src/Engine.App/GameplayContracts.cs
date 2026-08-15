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
        if (definition.Id.Value is null || state.Active || state.Complete) return false;
        state = new QuestState { Id = definition.Id, Active = true };
        return true;
    }

    public static bool RecordKill(ref QuestState state, EnemyId enemy, in QuestDefinition definition)
    {
        return RecordObjective(ref state, enemy, default, QuestObjectiveType.KillEnemy, in definition);
    }

    public static bool RecordTalk(ref QuestState state, NpcId npc, in QuestDefinition definition)
    {
        return RecordObjective(ref state, default, npc, QuestObjectiveType.TalkToNpc, in definition);
    }

    public static bool IsComplete(in QuestState state, in QuestDefinition definition)
    {
        if (!state.Active || state.Id != definition.Id) return false;
        QuestObjectiveDefinition firstObjective = definition.First;
        QuestObjectiveDefinition secondObjective = definition.Second;
        return IsObjectiveComplete(state.FirstProgress, in firstObjective) && IsObjectiveComplete(state.SecondProgress, in secondObjective);
    }

    public static bool CompleteReturn(ref QuestState state, NpcId npc, in QuestDefinition definition)
    {
        QuestObjectiveDefinition firstObjective = definition.First;
        QuestObjectiveDefinition secondObjective = definition.Second;
        if (!state.Active || state.Id != definition.Id || secondObjective.Type != QuestObjectiveType.ReturnToQuestGiver || secondObjective.Npc != npc || !IsObjectiveComplete(state.FirstProgress, in firstObjective)) return false;
        state.SecondProgress = Math.Max(1, secondObjective.Required);
        if (!IsComplete(in state, in definition)) return false;
        state.Complete = true;
        state.Active = false;
        return true;
    }

    public static bool TryClaimReward(ref QuestState state, in QuestDefinition definition, out QuestReward reward)
    {
        reward = default;
        if (!state.Complete || state.Id != definition.Id || state.RewardClaimed) return false;
        state.RewardClaimed = true;
        reward = definition.Reward;
        return true;
    }

    private static bool RecordObjective(ref QuestState state, EnemyId enemy, NpcId npc, QuestObjectiveType objectiveType, in QuestDefinition definition)
    {
        if (!state.Active || state.Id != definition.Id) return false;
        QuestObjectiveDefinition firstObjective = definition.First;
        QuestObjectiveDefinition secondObjective = definition.Second;
        if (Matches(in firstObjective, enemy, npc, objectiveType) && !IsObjectiveComplete(state.FirstProgress, in firstObjective))
        {
            state.FirstProgress = Math.Min(definition.First.Required, state.FirstProgress + 1);
            return true;
        }
        if (Matches(in secondObjective, enemy, npc, objectiveType) && !IsObjectiveComplete(state.SecondProgress, in secondObjective))
        {
            state.SecondProgress = Math.Min(definition.Second.Required, state.SecondProgress + 1);
            return true;
        }
        return false;
    }

    private static bool Matches(in QuestObjectiveDefinition objective, EnemyId enemy, NpcId npc, QuestObjectiveType objectiveType)
        => objective.Type == objectiveType && (objectiveType == QuestObjectiveType.KillEnemy ? objective.Enemy == enemy : objective.Npc == npc);

    private static bool IsObjectiveComplete(int progress, in QuestObjectiveDefinition objective)
        => objective.Required <= 0 || progress >= objective.Required;
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

    public static void EvaluateCompanion(ref AiState state, CompanionTactics tactics, Vector2 companionPosition, Vector2 ownerPosition, float healthPercent, Entity owner, Entity target, Vector2 targetPosition)
    {
        float ownerDistanceSquared = Vector2.DistanceSquared(companionPosition, ownerPosition);
        bool isOutsideOwnerDistance = ownerDistanceSquared > 25f;
        bool hasTarget = target.IsValid;
        bool hasSupportSkill = state.Definition.SupportSkill.Value is not null;
        switch (tactics)
        {
            case CompanionTactics.Support:
                if (hasSupportSkill && healthPercent < 0.5f) { state.Intent = new AiIntent { Kind = AiIntentKind.Cast, Skill = state.Definition.SupportSkill, Target = owner }; return; }
                if (hasTarget) { state.Intent = new AiIntent { Kind = AiIntentKind.Attack, Target = target }; return; }
                break;
            case CompanionTactics.Defensive:
                if (healthPercent <= state.Definition.FleeHealthPercent && hasTarget) { state.Intent = new AiIntent { Kind = AiIntentKind.Flee, Target = target, Destination = targetPosition }; return; }
                break;
            case CompanionTactics.FocusPlayerTarget:
                if (hasTarget) { state.Intent = new AiIntent { Kind = AiIntentKind.Attack, Target = target }; return; }
                break;
            case CompanionTactics.Aggressive:
            case CompanionTactics.Ranged:
                if (hasTarget) { state.Intent = new AiIntent { Kind = AiIntentKind.Attack, Target = target }; return; }
                break;
            case CompanionTactics.StayClose:
            case CompanionTactics.ProtectPlayer:
                if (isOutsideOwnerDistance) { state.Intent = new AiIntent { Kind = AiIntentKind.Follow, Target = owner, Destination = ownerPosition }; return; }
                if (hasTarget) { state.Intent = new AiIntent { Kind = AiIntentKind.Attack, Target = target }; return; }
                break;
        }
        state.Intent = isOutsideOwnerDistance
            ? new AiIntent { Kind = AiIntentKind.Follow, Target = owner, Destination = ownerPosition }
            : new AiIntent { Kind = AiIntentKind.Guard, Target = owner };
    }
}

public static class AiActionMapper
{
    public static CharacterIntent ToCharacterIntent(in AiIntent intent, Vector2 actorPosition)
    {
        return intent.Kind switch
        {
            AiIntentKind.MoveTo when IsFinite(intent.Destination) => new CharacterIntent(CharacterIntentKind.MoveTo, default, intent.Destination, default, default, default),
            AiIntentKind.Follow when intent.Target.IsValid => new CharacterIntent(CharacterIntentKind.Follow, default, intent.Destination, intent.Target, default, default),
            AiIntentKind.Attack when intent.Target.IsValid => new CharacterIntent(CharacterIntentKind.Attack, default, default, intent.Target, default, default),
            AiIntentKind.Cast when intent.Target.IsValid && intent.Skill.Value is not null => new CharacterIntent(CharacterIntentKind.Cast, default, default, intent.Target, intent.Skill, default),
            AiIntentKind.Flee when intent.Target.IsValid && IsFinite(intent.Destination) => CreateFleeIntent(actorPosition, intent.Destination),
            AiIntentKind.Interact when intent.Target.IsValid => new CharacterIntent(CharacterIntentKind.Interact, default, default, intent.Target, default, default),
            _ => new CharacterIntent(CharacterIntentKind.Stop, default, default, default, default, default)
        };
    }

    private static CharacterIntent CreateFleeIntent(Vector2 actorPosition, Vector2 threatPosition)
    {
        Vector2 direction = actorPosition - threatPosition;
        if (direction.LengthSquared() < 0.0001f) return new CharacterIntent(CharacterIntentKind.Stop, default, default, default, default, default);
        return new CharacterIntent(CharacterIntentKind.Move, Vector2.Normalize(direction), default, default, default, default);
    }

    private static bool IsFinite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);
}
