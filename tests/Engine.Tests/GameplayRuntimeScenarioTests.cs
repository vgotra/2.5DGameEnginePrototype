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
        new(nameof(Ai_ChoosesSupportFollowAndAttackIntents), Ai_ChoosesSupportFollowAndAttackIntents),
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

    private static void GameContent_DefinesScenesAndQuest()
    {
        QuestDefinition quest = GameContent.CreateGoblinProblem();
        TestAssert.True(quest.Id == GameContent.GoblinProblem && quest.Reward.Gold == 100, "goblin quest is defined");
        TestAssert.True(GameContent.VillageScene.Value != GameContent.GoblinForestScene.Value, "content has distinct scenes");
    }
}
