# Public Game API Refactor Roadmap

## Current status

The public authoring API is functional for the main scenario. Implemented foundations include:

- concrete `Game` composition root;
- `SceneManager`, `Scene`, `SceneParams`, typed IDs, and marker locations;
- `ContentRegistry` for heroes, enemies, NPCs, items, skills, effects, quests, scenes, and world-map locations;
- deferred public commands backed by the canonical sparse ECS;
- typed `Hero`, `Enemy`, `Npc`, `Item`, `Projectile`, and `Effect` handles;
- internal `IGameRuntimeBridge` for command application, fixed-step execution, and presentation extraction;
- sample runtime bridge and typed-handle-based progression/spawn setup;
- removal of the public `GameApplication.World` property;
- scene ownership checks for character-to-character casts;
- deterministic duplicate-content rejection;
- world-map content application during runtime-world creation.

## Next implementation slice

Implement these in order:

1. Require explicit environment configuration for public scenes before spawning.
   - Add internal environment-configured state to `Scene`.
   - Keep legacy `World.LoadScene` compatibility behavior temporarily.
   - Make `SceneManager.LoadScene` mark scenes as public-authoring scenes.
   - Reject spawning before `SetEnv` with a clear authoring exception.
   - Configure scene-definition environments before applying authored spawns.

2. Complete ownership safety for every public handle.
   - Add owning `Game`/`Scene` identity to `Item`, `Projectile`, and `Effect`.
   - Validate loaded scene, ownership, and target validity at every public operation.
   - Preserve pending handles: commands issued before the fixed-step boundary remain valid.
   - Ensure scene unload invalidates all handle categories safely.

3. Finish public runtime-leak removal.
   - Migrate remaining public callers away from `Game.ActiveWorld` and `World.Catalog`.
   - Keep compatibility members only while existing internal tests require them.
   - Mark compatibility members internal or obsolete after migration.
   - Verify public metadata does not expose ECS, Vulkan, SDL, `EntityCommands`, or raw `Entity` types.

4. Make presentation extraction fully real.
   - Configure the sample bridge with the actual presentation extraction callback.
   - Keep ECS extraction and `PresentationPositionHistory` inside the runtime bridge.
   - Run extraction after fixed-step simulation and before rendering.
   - Preserve interpolation and deterministic fixed-step ordering.

5. Add public contract tests.
   - Environment-required spawning.
   - Invalid marker and wrong-map rejection.
   - Cross-game and cross-scene handle rejection.
   - Pending-handle commands before activation.
   - Unloaded-scene invalidation for every handle type.
   - Duplicate registration for every content category.
   - Scene-definition loading and world-map registration.
   - Reflection/API audit for backend and ECS leaks.

## Following slices

6. Move sample registration into explicit modules:

- `SampleContent.Register(ContentRegistry)`;
- `SampleScenes.ConfigureVillage(Scene)`;
- `SampleScenes.ConfigureGoblinForest(Scene)`;
- `SampleScenario.SpawnVillageObjects(Scene)`;
- `SampleScenario.SpawnForestObjects(Scene)`.

7. Remove remaining sample authoring dependencies on `World`, `EntityCommands`, raw `Entity`, and ECS component access. Runtime systems may continue using those types behind `SampleRuntimeBridge`.

8. Complete scene transition sequencing: validate target, activate scene, queue scene-owned commands, apply at the fixed-step boundary, notify runtime systems, then update camera and presentation state.

9. Version and document the public contract. Increment `GameplayApiVersion` for breaking changes and maintain a short compatibility policy.

## Semantics to preserve

- Scene loading and scene starting are separate operations.
- Spawning and gameplay setup are deferred until command application.
- Commands are applied in submission order.
- Setup commands may be issued while handles are pending.
- Invalid commands must not partially mutate gameplay state.
- Scene-owned ECS entities are destroyed on unload.
- World-level objects survive scene transitions.
- Vulkan and SDL remain internal implementation details.
- The sparse ECS remains canonical; structural changes remain deferred.

## Compatibility constraints

- Existing engine tests still exercise `World`, `World.Catalog`, and raw ECS APIs.
- Do not remove those compatibility surfaces until internal callers and tests are migrated.
- `Game.WorldMap` is renderer-neutral and safe to retain.
- Legacy `World.LoadScene` currently creates a default environment for compatibility; public `SceneManager` behavior should become stricter first.
- Pending ECS entities are not alive before command application. Public handles must distinguish pending validity from active/alive state.

## Verification commands

Run from the repository root after code changes:

```text
dotnet build Engine.slnx --nologo
dotnet run --project tests\Engine.Tests\Engine.Tests.csproj
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --frames 60 --cap 0
git diff --check
```

Expected results: zero build errors, `Smoke tests passed`, a bounded successful sample run, and no diff-check errors. Run benchmarks only when ECS or another simulation hot path changes.

## Files to inspect first

- `src/Engine.App/Game.cs`
- `src/Engine.App/SceneManager.cs`
- `src/Engine.App/Scene.cs`
- `src/Engine.App/GameplayApi.cs`
- `src/Engine.App/ContentRegistry.cs`
- `src/Engine.App/GameRuntimeBridge.cs`
- `src/Engine.App/GameApplication.cs`
- `samples/IsometricSandbox/Game/Runtime/SampleRuntimeBridge.cs`
- `samples/IsometricSandbox/Game/Gameplay/Progression/GameProgression.cs`
- `samples/IsometricSandbox/Game/World/SampleEntitySpawner.cs`
- `tests/Engine.Tests/PublicGameplayApiTests.cs`
