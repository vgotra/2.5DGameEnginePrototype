# 2.5D Isometric Game Engine

Windows-first .NET 10 prototype using SDL3, Vulkan, and a sparse-set ECS. The runtime is an isometric 2.5D engine with explicit frame scheduling and renderer-neutral gameplay contracts.

## Build and run

```powershell
dotnet build Engine.slnx
dotnet run --project samples\IsometricSandbox\IsometricSandbox.csproj -- --frames 10 --cap 0
```

The bounded sample command is suitable for automated verification. Omit `--frames` only for an interactive run.

## Public gameplay API

Game code can use typed domain handles and leave sparse ECS details to the engine:

```csharp
Hero rogue = scene.SpawnHero(
    HeroIds.Rogue,
    scene.Map.Resolve("ForestEntrance"));

rogue.Inventory.Add(ItemIds.GoblinSlayerBow);
rogue.Equipment.Equip(
    EquipmentSlot.MainHand,
    ItemIds.GoblinSlayerBow);

rogue.Skills.Learn(SkillIds.PowerShot);
rogue.Skills.Learn(SkillIds.PoisonArrow);

Enemy shaman = scene.SpawnEnemy(
    EnemyIds.GoblinShaman,
    scene.Map.Resolve("GoblinCamp"));

rogue.Cast(SkillIds.PoisonArrow, shaman);
```

Content IDs and definitions are registered through `GameplayCatalog`. Inventory,
equipment, skill, and cast calls are deferred until the next fixed-step command
boundary. Applications are created through the desktop composition root and expose
only `GameApplication`, `GameContext`, `Scene`, `RenderContext`, and typed gameplay
services. `Engine.Ecs.Sparse`, SDL3, native surfaces, Vulkan, and low-level renderer
types are implementation or advanced integration surfaces, not required for
ordinary gameplay code.

## Benchmarks

`benchmarks\Engine.Benchmark` is retained for opt-in performance-regression checks. Benchmark results are machine-sensitive; use the project’s documented comparison and baseline commands when investigating performance changes.

Project-specific agent instructions are in [`AGENTS.md`](AGENTS.md). Durable implementation notes are limited to the context files that remain in `.agents\context`.
