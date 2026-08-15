using Engine.App;
using Engine.Ecs.Sparse;
using System.Numerics;

namespace Engine.Tests;

internal static class GameplayRuntimeScenarioTests
{
    internal static readonly TestCase[] Tests =
    [
        new(nameof(Effects_StackAndExpireDeterministically), Effects_StackAndExpireDeterministically),
        new(nameof(Quest_KillAndReturnProgressCompletes), Quest_KillAndReturnProgressCompletes),
        new(nameof(Quest_TalkAndRewardClaimAreDeterministic), Quest_TalkAndRewardClaimAreDeterministic),
        new(nameof(Ai_ChoosesSupportFollowAndAttackIntents), Ai_ChoosesSupportFollowAndAttackIntents),
        new(nameof(Ai_MapsIntentsToSafeCharacterActions), Ai_MapsIntentsToSafeCharacterActions),
        new(nameof(CompanionTactics_SelectDeterministicPriorities), CompanionTactics_SelectDeterministicPriorities),
        new(nameof(GameContent_DefinesScenesAndQuest), GameContent_DefinesScenesAndQuest)
    ];

    private static void Effects_StackAndExpireDeterministically()
    {
        ActiveEffects effects = default;
        GameplayEffectDefinition poison = new(GameContent.Poison, 2, 1, 3, StatId.Strength, 0, EffectStackPolicy.Stack, default, default);
        TestAssert.True(EffectRuntime.Apply(ref effects, in poison, default), "effect applies");
        TestAssert.True(EffectRuntime.Apply(ref effects, in poison, default) && effects.First.Stacks == 2, "stack policy increments");
        EffectRuntime.Tick(ref effects);
        EffectRuntime.Tick(ref effects);
        TestAssert.True(effects.First.Id.Value is null, "effect expires after duration");
    }

    private static void Quest_KillAndReturnProgressCompletes()
    {
        QuestDefinition definition = GameContent.CreateGoblinProblem();
        QuestState state = default;
        TestAssert.True(QuestRuntime.Activate(ref state, in definition), "quest activates");
        QuestRuntime.RecordKill(ref state, GameContent.GoblinWarrior, in definition);
        QuestRuntime.RecordKill(ref state, GameContent.GoblinWarrior, in definition);
        QuestRuntime.RecordKill(ref state, GameContent.GoblinWarrior, in definition);
        TestAssert.True(QuestRuntime.CompleteReturn(ref state, GameContent.ElderMarcus, in definition) && state.Complete, "quest completes on return");
    }

    private static void Ai_ChoosesSupportFollowAndAttackIntents()
    {
        AiState state = new() { Definition = new AiDefinition(AiIntentKind.Idle, 8f, 0.2f, new SkillId("heal")) };
        Entity owner = new(1, 1);
        AiRuntime.Evaluate(ref state, Vector2.Zero, new Vector2(10, 0), 0.3f, owner, default);
        TestAssert.True(state.Intent.Kind == AiIntentKind.Cast, "low health selects support cast");
        AiRuntime.Evaluate(ref state, Vector2.Zero, new Vector2(10, 0), 1f, owner, default);
        TestAssert.True(state.Intent.Kind == AiIntentKind.Follow, "separated companion follows owner");
        AiRuntime.Evaluate(ref state, Vector2.Zero, Vector2.Zero, 1f, owner, new Entity(2, 1));
        TestAssert.True(state.Intent.Kind == AiIntentKind.Attack, "target selects attack");
    }

    private static void Quest_TalkAndRewardClaimAreDeterministic()
    {
        QuestDefinition definition = new(
            new QuestId("talk-quest"),
            new QuestObjectiveDefinition(QuestObjectiveType.TalkToNpc, default, GameContent.ElderMarcus, 1),
            new QuestObjectiveDefinition(QuestObjectiveType.ReturnToQuestGiver, default, GameContent.ElderMarcus, 1),
            new QuestReward(GameContent.GoblinSlayerBow, 25));
        QuestState state = default;
        TestAssert.True(QuestRuntime.Activate(ref state, in definition), "talk quest activates");
        TestAssert.True(!QuestRuntime.RecordTalk(ref state, new NpcId("wrong"), in definition), "wrong NPC is rejected");
        TestAssert.True(QuestRuntime.RecordTalk(ref state, GameContent.ElderMarcus, in definition), "talk objective progresses");
        TestAssert.True(QuestRuntime.CompleteReturn(ref state, GameContent.ElderMarcus, in definition), "return completes quest");
        TestAssert.True(QuestRuntime.TryClaimReward(ref state, in definition, out QuestReward reward) && reward.Gold == 25, "completed quest pays reward");
        TestAssert.True(!QuestRuntime.TryClaimReward(ref state, in definition, out _), "reward cannot be claimed twice");
    }

    private static void Ai_MapsIntentsToSafeCharacterActions()
    {
        Entity target = new(4, 1);
        AiIntent attack = new() { Kind = AiIntentKind.Attack, Target = target };
        CharacterIntent attackAction = AiActionMapper.ToCharacterIntent(in attack, Vector2.Zero);
        TestAssert.True(attackAction.Kind == CharacterIntentKind.Attack && attackAction.Target == target, "attack intent maps to attack action");
        AiIntent cast = new() { Kind = AiIntentKind.Cast, Target = target, Skill = new SkillId("heal") };
        TestAssert.True(AiActionMapper.ToCharacterIntent(in cast, Vector2.Zero).Kind == CharacterIntentKind.Cast, "cast intent maps to cast action");
        AiIntent flee = new() { Kind = AiIntentKind.Flee, Target = target, Destination = new Vector2(1, 0) };
        CharacterIntent fleeAction = AiActionMapper.ToCharacterIntent(in flee, Vector2.Zero);
        TestAssert.True(fleeAction.Kind == CharacterIntentKind.Move && fleeAction.Direction.LengthSquared() > 0.99f, "flee intent maps to normalized movement");
        AiIntent invalid = new() { Kind = AiIntentKind.Cast, Target = default };
        TestAssert.True(AiActionMapper.ToCharacterIntent(in invalid, Vector2.Zero).Kind == CharacterIntentKind.Stop, "invalid cast safely stops");
        AiIntent patrol = new() { Kind = AiIntentKind.Patrol };
        TestAssert.True(AiActionMapper.ToCharacterIntent(in patrol, Vector2.Zero).Kind == CharacterIntentKind.Stop, "patrol fallback is deterministic");
    }

    private static void CompanionTactics_SelectDeterministicPriorities()
    {
        Entity owner = new(1, 1);
        Entity target = new(2, 1);
        AiState state = new() { Definition = new AiDefinition(AiIntentKind.Follow, 8f, 0.2f, new SkillId("heal")) };
        AiRuntime.EvaluateCompanion(ref state, CompanionTactics.Support, Vector2.Zero, Vector2.Zero, 0.3f, owner, target, new Vector2(2, 0));
        TestAssert.True(state.Intent.Kind == AiIntentKind.Cast && state.Intent.Target == owner, "support tactic casts on wounded owner");
        AiRuntime.EvaluateCompanion(ref state, CompanionTactics.Defensive, Vector2.Zero, Vector2.Zero, 0.1f, owner, target, new Vector2(2, 0));
        TestAssert.True(state.Intent.Kind == AiIntentKind.Flee && state.Intent.Destination == new Vector2(2, 0), "defensive tactic retreats from target");
        AiRuntime.EvaluateCompanion(ref state, CompanionTactics.Aggressive, Vector2.Zero, new Vector2(10, 0), 1f, owner, target, default);
        TestAssert.True(state.Intent.Kind == AiIntentKind.Attack, "aggressive tactic attacks target");
        AiRuntime.EvaluateCompanion(ref state, CompanionTactics.StayClose, Vector2.Zero, new Vector2(10, 0), 1f, owner, target, default);
        TestAssert.True(state.Intent.Kind == AiIntentKind.Follow, "stay-close tactic follows owner first");
        AiRuntime.EvaluateCompanion(ref state, CompanionTactics.FocusPlayerTarget, Vector2.Zero, Vector2.Zero, 1f, owner, default, default);
        TestAssert.True(state.Intent.Kind == AiIntentKind.Guard, "focus tactic guards without a target");
    }

    private static void GameContent_DefinesScenesAndQuest()
    {
        QuestDefinition quest = GameContent.CreateGoblinProblem();
        TestAssert.True(quest.Id == GameContent.GoblinProblem && quest.Reward.Gold == 100, "goblin quest is defined");
        TestAssert.True(GameContent.VillageScene.Value != GameContent.GoblinForestScene.Value, "content has distinct scenes");
    }
}
