# 2.5DGameEnginePrototype — Simplified Runtime Architecture v2

## 1. Updated Goal

Simplify the existing engine without turning it into a single-threaded toy engine.

The target remains a serious C# 2D/2.5D isometric engine capable of games such as:

- Diablo-like ARPG
- isometric RPG
- tactical RPG
- action adventure
- potentially simpler narrative/adventure games

Typical runtime scale:

```text
1 active Game
1 active World
1 active Scene normally
100–500 gameplay entities
hundreds/thousands of lightweight projectiles/effects
background asset work
render preparation
audio
pathfinding
```

The engine MUST support multithreading.

However:

> Multithreading is an engine capability, not a requirement that every gameplay system must execute in parallel.

The engine should automatically avoid parallel execution when scheduling overhead would cost more than the work itself.

---

# 2. Target Runtime Model

Introduce an explicit hierarchy:

```text
Application
    │
    ▼
Game
    │
    ▼
World
    │
    ▼
Scene
    │
    ├── Entities
    ├── Components
    ├── Systems
    ├── Camera
    ├── Environment
    └── Scene resources
```

Responsibilities must be obvious.

---

# 3. Application

`Application` owns engine/platform lifetime.

Responsibilities:

```text
Application
├── Platform
├── Window
├── Input
├── Renderer
├── Audio
├── AssetManager
├── JobSystem
└── Game
```

Application should know nothing about:

```text
Goblin
Paladin
Damage
Inventory
Quest
```

---

# 4. Game

`Game` represents the actual game.

Example:

```csharp
public sealed class SampleGame : Game
{
    protected override void Initialize()
    {
        LoadWorld("Sanctuary");
    }
}
```

Game owns:

```text
Game
├── World(s)
├── GameState
├── Game rules
├── Global assets
└── Save state
```

The API should eventually allow:

```csharp
game.LoadWorld("Sanctuary");

game.ChangeScene("Cathedral");

game.Save();
```

---

# 5. World

`World` represents a persistent gameplay world.

It owns:

```text
World
├── Entity registry
├── Component stores
├── Systems
├── Scenes
├── Persistent entities
└── World state
```

Example:

```text
World: Sanctuary

├── Tristram
├── Cathedral
├── Crypt
└── Dungeon
```

Changing Scene does NOT require destroying the entire World.

---

# 6. Scene

A Scene represents the currently loaded gameplay area.

Example:

```text
Scene
├── TileMap
├── Scene entities
├── Spawn points
├── Lights
├── Camera
├── Navigation
├── Static geometry
├── Audio emitters
└── Scene resources
```

Example API:

```csharp
var scene = world.LoadScene("Cathedral");

scene.Spawn("Goblin", position);

scene.Spawn("GoblinShaman", position);
```

Scene should NOT become another ECS.

It is an ownership/lifetime boundary.

---

# 7. Entity Lifetime

Entities should have an explicit lifetime category:

```text
Scene
World
Transient
```

Examples:

```text
Goblin
    → Scene

Dropped legendary item
    → Scene

Player
    → World

Quest state
    → World/GameState

Fireball
    → Transient
```

Scene unload can therefore efficiently destroy Scene-owned entities.

---

# 8. ECS Philosophy

KEEP ECS.

KEEP data-oriented components.

REMOVE archetype ECS.

Target:

```text
Entity
+
ComponentStore<T>
+
Simple Query
+
System
```

Do NOT replace ECS with inheritance-heavy GameObjects.

---

# 9. Remove Archetype ECS

Remove:

```text
Archetype
ArchetypeKey
EntityLocation → Archetype
archetype migration
archetype table management
archetype query cache
component-signature migration
```

Changing components must become trivial.

Instead of:

```text
Entity
  ↓
Archetype A

Add<Poisoned>()

  ↓
find/create Archetype B
  ↓
move entity
  ↓
copy components
  ↓
update EntityLocation
```

target:

```text
Add<Poisoned>()

    ↓

PoisonedStore.Add(entity)
```

This is the central ECS simplification.

---

# 10. Entity

Keep Entity extremely small:

```csharp
public readonly struct Entity
{
    public readonly int Id;
    public readonly int Generation;
}
```

Generation protects against stale handles.

No component signature should live in `Entity`.

No archetype reference.

No Scene object reference.

---

# 11. Component Storage

Use independent typed component stores:

```text
World
│
├── ComponentStore<Transform>
├── ComponentStore<Velocity>
├── ComponentStore<Sprite>
├── ComponentStore<Health>
├── ComponentStore<Enemy>
├── ComponentStore<Collider>
└── ...
```

Recommended internal representation:

```text
dense components[]
dense entities[]
sparse entity → dense index
```

This gives:

- dense iteration
- good cache locality
- cheap add/remove
- cheap lookup
- simple implementation
- no archetype migration

---

# 12. ComponentStore API

Target approximately:

```csharp
public sealed class ComponentStore<T>
    where T : struct
{
    public bool Has(Entity entity);

    public ref T Get(Entity entity);

    public bool TryGet(Entity entity, out T value);

    public void Add(Entity entity, in T component);

    public void Remove(Entity entity);
}
```

World provides convenience:

```csharp
world.Add(entity, new Health(100));

ref var health = ref world.Get<Health>(entity);

world.Remove<Poisoned>(entity);
```

---

# 13. Simple Queries

Queries remain data-oriented.

Example:

```csharp
foreach (var entity in world.Query<Transform, Velocity>())
{
    ref var transform = ref world.Get<Transform>(entity);
    ref var velocity = ref world.Get<Velocity>(entity);

    transform.Position += velocity.Value * dt;
}
```

Internally:

```text
select smallest ComponentStore
        ↓
iterate dense entities
        ↓
check remaining stores
        ↓
execute
```

For hundreds or a few thousand entities this is sufficient.

---

# 14. No Archetype Query Cache

Do NOT rebuild archetype complexity through another mechanism.

Avoid:

```text
QuerySignature
QueryPlan
CachedArchetypeSet
ArchetypeMask
```

A query should be cheap and obvious.

If profiling eventually shows that a particular query is expensive:

optimize THAT query.

Do not redesign ECS globally.

---

# 15. Systems Remain

Keep systems.

Example:

```text
InputSystem
PlayerSystem
AISystem
MovementSystem
CombatSystem
ProjectileSystem
AnimationSystem
LifetimeSystem
```

A system is simply code operating on World.

Example:

```csharp
public sealed class MovementSystem
{
    public void Update(World world, float dt)
    {
        foreach (var entity in world.Query<Transform, Velocity>())
        {
            ...
        }
    }
}
```

No mandatory base class should be required unless it provides real value.

---

# 16. Scheduler: KEEP It, Remove Intelligence

Do NOT delete the scheduler.

Replace the current intelligent scheduler concept with a small explicit frame scheduler.

REMOVE:

```text
automatic component read/write inference
automatic dependency graph construction
runtime conflict analysis
complex system DAG
scheduler deciding gameplay semantics
```

KEEP:

```text
ordered stages
parallel groups
explicit dependencies when genuinely needed
job submission
barriers
profiling
```

---

# 17. Frame Pipeline

Create explicit stages:

```text
FRAME

Input
  ↓
PreUpdate
  ↓
Gameplay
  ↓
Physics
  ↓
PostUpdate
  ↓
RenderPrepare
  ↓
Render
```

Example:

```csharp
scheduler.Stage(FrameStage.Gameplay)
    .Add(playerSystem)
    .Add(aiSystem)
    .Add(combatSystem);

scheduler.Stage(FrameStage.Physics)
    .Add(movementSystem)
    .Add(collisionSystem);
```

Ordering is explicit.

A developer should understand frame ordering by reading this configuration.

---

# 18. Explicit Parallel Groups

Allow explicit parallelism:

```csharp
scheduler.Stage(FrameStage.Gameplay)
    .Parallel(
        animationSystem,
        particleSimulationSystem,
        independentAiSystem);
```

But do NOT automatically infer:

```text
System A reads Transform
System B writes Health
therefore...
```

The developer specifies safe parallel groups.

This dramatically simplifies scheduler behavior.

---

# 19. Optional Explicit Dependencies

If needed, support:

```csharp
scheduler.Add(pathfinding)
    .Before(movement);
```

or:

```csharp
scheduler.Add(combat)
    .After(movement);
```

But this should compile/build into a simple schedule.

Do NOT continuously perform complicated dependency inference during every frame.

---

# 20. Multithreading Is Required

The engine MUST have a worker pool.

Architecture:

```text
                  MAIN THREAD
                      │
         ┌────────────┼─────────────┐
         │            │             │
      Gameplay     Scheduler     Renderer
                      │
                      ▼
                  Job System
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
       Worker 1    Worker 2    Worker N
```

Do NOT use:

```text
Task.Run
ThreadPool.QueueUserWorkItem
new Thread
```

throughout gameplay code.

One engine JobSystem owns worker execution.

---

# 21. Worker Count

Default approximately:

```text
workerCount =
    max(1, Environment.ProcessorCount - reservedThreads)
```

But cap or tune it where appropriate.

Do not assume that using every hardware thread is always optimal.

Mobile will require more conservative behavior.

---

# 22. Adaptive Parallelism

This is important.

Parallelism should have a workload threshold.

Example:

```csharp
if (entityCount < ParallelThreshold)
{
    UpdateSerial();
}
else
{
    UpdateParallel();
}
```

Conceptually:

```text
20 enemies
    ↓
SERIAL

200 enemies
    ↓
probably SERIAL or few jobs

1000 lightweight entities
    ↓
PARALLEL

10,000 particles
    ↓
PARALLEL
```

The exact threshold MUST come from benchmarks.

---

# 23. Parallel Query API

Provide parallel processing as an OPTIONAL query operation.

Example:

```csharp
world.Query<Transform, Velocity>()
     .ParallelForEach(
         jobs,
         static (ref Transform transform,
                 ref Velocity velocity,
                 float dt) =>
         {
             transform.Position += velocity.Value * dt;
         },
         dt);
```

Internally it should:

```text
determine count
     ↓
below threshold?
 ┌───────┴────────┐
YES               NO
 ↓                 ↓
serial         split into chunks
                   ↓
              submit few jobs
```

---

# 24. Never One Job Per Entity

Forbidden:

```text
500 enemies
=
500 jobs
```

Instead:

```text
500 enemies

Worker 1 → 0..124
Worker 2 → 125..249
Worker 3 → 250..374
Worker 4 → 375..499
```

Chunk size should be configurable/tunable.

---

# 25. Parallelism Categories

Classify work into three categories.

## A. Always Main Thread

Usually:

```text
Input
Game state transitions
Scene switching
Entity structural changes
small gameplay systems
```

## B. Adaptive

Examples:

```text
AI
movement
animation
collision broad phase
projectiles
particles
visibility
```

Serial for small workloads.

Parallel for sufficiently large workloads.

## C. Naturally Background

Examples:

```text
asset loading
texture decoding
map loading
pathfinding requests
procedural generation
resource preparation
```

These should normally use worker threads.

---

# 26. Structural ECS Changes

Avoid mutating component stores while another worker is iterating them.

During parallel processing:

```text
READ/WRITE COMPONENT DATA
        ↓
parallel jobs
        ↓
barrier
        ↓
apply structural changes
```

Structural changes:

```text
Create entity
Destroy entity
Add component
Remove component
```

should normally occur at synchronization points.

---

# 27. Keep a Tiny Command Buffer

Therefore DO NOT necessarily delete `WorldCommandBuffer`.

Instead simplify it radically.

Rename if useful:

```text
EntityCommands
```

Only support:

```text
Create
Destroy
Add<T>
Remove<T>
```

Example:

```csharp
commands.Destroy(enemy);

commands.Add(enemy, new Dead());
```

After parallel systems finish:

```csharp
commands.Apply(world);
```

No scheduler intelligence should live in this object.

---

# 28. Scheduler + JobSystem Separation

These must be separate concepts.

```text
FrameScheduler
     │
     │ decides WHEN
     ▼
System

JobSystem
     │
     │ decides WHERE work executes
     ▼
Workers
```

Scheduler:

```text
ordering
stages
barriers
parallel groups
```

JobSystem:

```text
worker threads
queues
work stealing if retained
job execution
waiting
```

Do NOT combine them into one giant subsystem.

---

# 29. Work Stealing

Existing work stealing MAY remain.

Do not remove a working implementation merely for ideological simplicity.

Evaluate:

```text
LOC
complexity
correctness
benchmark benefit
maintenance cost
```

If it is small and reliable:

KEEP IT.

If it dominates Engine.Threading complexity without measurable benefit:

replace with simpler worker queues.

The simplification target is not:

> remove sophisticated algorithms.

It is:

> remove sophistication that does not pay for itself.

---

# 30. Game-Level API Must Hide ECS Complexity

Raw ECS is an internal engine mechanism.

Game code should increasingly look like:

```csharp
var player = game.SpawnHero(
    HeroType.Paladin,
    position);

var shaman = scene.SpawnMonster(
    MonsterType.GoblinShaman,
    position);
```

not:

```csharp
var entity = world.Create();

world.Add(entity, new Transform(...));
world.Add(entity, new Health(...));
world.Add(entity, new Enemy(...));
world.Add(entity, new Sprite(...));
world.Add(entity, new Ai(...));
```

Raw ECS remains available for engine/system code.

---

# 31. Game / World / Scene Example

Desired eventual API:

```csharp
public sealed class Sample : Game
{
    protected override void Start()
    {
        var world = CreateWorld("Sanctuary");

        var scene = world.LoadScene("Tristram");

        var player = scene.SpawnHero(
            HeroType.Paladin,
            new(10, 12));

        scene.SpawnMonster(
            MonsterType.Goblin,
            new(20, 18));

        scene.SpawnMonster(
            MonsterType.GoblinShaman,
            new(23, 19));
    }
}
```

This is the level at which a game developer should work.

---

# 32. World Update

World should expose one obvious update path:

```csharp
public void Update(float dt)
{
    scheduler.Run(FrameStage.PreUpdate, this, dt);

    scheduler.Run(FrameStage.Gameplay, this, dt);

    scheduler.Run(FrameStage.Physics, this, dt);

    ApplyEntityCommands();

    scheduler.Run(FrameStage.PostUpdate, this, dt);
}
```

Avoid hidden lifecycle magic.

---

# 33. Scene Update

Scene should primarily represent:

```text
ownership
resources
environment
navigation
spawning
lifetime
```

Do NOT duplicate World systems inside every Scene.

Systems generally operate on World and filter by active Scene where necessary.

---

# 34. Persistent Entities

Some entities survive scene transitions.

Example:

```text
Player
companions
persistent projectiles? usually no
global quest actors
```

World owns these.

Scene change:

```text
Unload Cathedral
       ↓
destroy Scene-owned entities
       ↓
preserve World-owned entities
       ↓
Load Crypt
       ↓
spawn Crypt entities
       ↓
move Player into Crypt
```

---

# 35. Rendering

Rendering remains separate.

After gameplay:

```text
World
  ↓
RenderExtraction
  ↓
RenderWorld / RenderList
  ↓
Renderer
  ↓
Vulkan
```

Render extraction can become parallel if profiling shows value.

The renderer does not directly iterate arbitrary gameplay state.

---

# 36. Recommended Final Architecture

```text
Application
│
├── Platform / SDL3
├── Input
├── Assets
├── Audio
├── JobSystem ────────────────┐
├── Renderer                  │
│                             │
└── Game                      │
     │                        │
     ▼                        │
    World                     │
     │                        │
     ├── Scene                │
     │                        │
     ├── Entity Registry      │
     │                        │
     ├── ComponentStore<T>    │
     │                        │
     └── FrameScheduler ──────┘
              │
              ├── Input
              ├── AI
              ├── Movement
              ├── Combat
              ├── Physics
              ├── Animation
              └── RenderExtraction
```

---

# 39. Milestone 2 — New Sparse ECS Core

Implement alongside old ECS:

```text
Entity
EntityRegistry
ComponentStore<T>
World.Add<T>
World.Remove<T>
World.Get<T>
World.Has<T>
```

No archetypes.

No scheduler integration yet.

Benchmark component operations.

---

# 40. Milestone 3 — Queries

Implement simple dense-store queries.

Required:

```text
Query<T>
Query<T1,T2>
Query<T1,T2,T3>
```

Avoid separate duplicated implementations where possible.

Add:

```text
Count
serial iteration
```

No parallel implementation yet.

Benchmark:

```text
100
500
1000
5000
100000
```

---

# 41. Milestone 4 — Explicit Frame Scheduler

Replace scheduler intelligence.

Create:

```text
FrameScheduler
FrameStage
SystemRegistration
ParallelGroup
Barrier
```

Remove:

```text
automatic ComponentAccess analysis
automatic read/write conflict detection
runtime dependency inference
```

Support explicit:

```text
ordering
stages
parallel groups
```

Success:

The complete update order can be understood from one configuration.

---

# 42. Milestone 5 — Port Existing Systems

Move existing gameplay/sample systems onto:

```text
new World
new ComponentStore
new Query
new FrameScheduler
```

Initially run serially.

Compare against old behavior.

Success:

IsometricSandbox works correctly without archetypes.

---

# 43. Milestone 6 — Remove Archetype ECS

Delete:

```text
Archetype
ArchetypeKey
archetype migration
archetype query cache
old EntityLocation archetype tracking
old QueryParallelDispatch architecture
```

Do NOT retain compatibility wrappers.

Success:

Production code contains no archetype dependency.

---

# 44. Milestone 7 — Multithreaded Queries

Now add multithreading back deliberately.

Implement:

```text
ParallelFor
ParallelForEach
chunk partitioning
threshold-based serial fallback
```

Use existing JobSystem where practical.

Benchmark thresholds.

Example result might become:

```text
< 512 elements
    → serial

512–2048
    → benchmark-dependent

> 2048
    → parallel
```

DO NOT hardcode these numbers until measured.

---

# 45. Milestone 8 — Simplify JobSystem

Only now audit existing threading.

Keep:

```text
worker pool
efficient queues
work stealing if justified
thread-local scratch if useful
job handles
Wait/WhenAll
```

Remove:

```text
ECS-specific scheduler coupling
unnecessary dependency machinery
features without callers
```

Target public API approximately:

```csharp
JobHandle Run(Action job);

JobHandle ParallelFor(
    int count,
    Action<int, int> rangeJob);

void Wait(JobHandle handle);
```

---

# 46. Milestone 9 — Structural Command Buffer

Simplify `WorldCommandBuffer`.

Target:

```text
EntityCommands
```

Only:

```text
Create
Destroy
Add
Remove
```

Use it when structural ECS mutations must be deferred across parallel execution.

Serial gameplay code may perform immediate structural changes when safe.

---

# 47. Milestone 10 — Gameplay API

Introduce:

```text
HeroDefinition
MonsterDefinition
WeaponDefinition
SkillDefinition

SpawnHero
SpawnMonster
SpawnProjectile
SpawnItem
```

Game code should stop looking like an ECS test.

---

# 48. Milestone 11 — Realistic ARPG Benchmark

Build a representative workload:

```text
1 player

250 monsters

100 projectiles

500 particles/effects

AI

movement

collision

combat

animation

rendering
```

Measure:

```text
serial gameplay
adaptive parallel gameplay
forced parallel gameplay
```

This benchmark becomes more important than the 100k Critter benchmark.

---

# 49. Milestone 12 — Tune Multithreading

For each system classify:

```text
SERIAL
ADAPTIVE
PARALLEL
BACKGROUND
```

Example expected result:

| System | Likely Mode |
|---|---|
| Input | Serial |
| Player | Serial |
| Quest | Serial |
| Scene management | Serial |
| AI | Adaptive |
| Movement | Adaptive |
| Collision | Adaptive |
| Animation | Adaptive |
| Particles | Parallel |
| Asset loading | Background |
| Texture decoding | Background |
| Pathfinding | Background/Parallel |
| Render preparation | Adaptive |

These are starting assumptions only.

Benchmark before finalizing.

---

# 50. Milestone 13 — Rendering Audit

Only after runtime simplification is stable:

benchmark Vulkan command preparation.

Keep renderer multithreading if useful.

Simplify it if realistic workloads show that threading overhead exceeds benefit.

Do NOT couple this milestone with ECS refactoring.

---

# 51. Milestone 14 — Delete and Document

Perform final deletion pass.

Update:

```text
README
GameEngineDesign
Roadmap
AGENTS.md
.agents/context
```

New architectural statement:

> The engine uses a simple sparse-set ECS with explicit frame scheduling and adaptive multithreading.

Remove documentation that instructs AI agents to recreate:

```text
archetypes
automatic component dependency analysis
mandatory parallel execution
```

---

# 52. Final Design Rule

The engine is:

```text
MULTITHREADED

but not

PARALLEL-BY-DEFAULT
```

More precisely:

```text
small workload
      ↓
serial

large independent workload
      ↓
parallel

blocking/background workload
      ↓
worker thread
```

Multithreading is selected because it improves frame time, not because the architecture requires demonstrating multithreading.

---

# 53. First Task for Coding Agent

Start with Milestone 0.

Before changing anything:

1. Inspect current ECS.
2. Inspect current scheduler.
3. Inspect current JobSystem.
4. Identify every archetype dependency.
5. Identify every `ComponentAccess` dependency.
6. Identify every parallel ECS query.
7. Identify every caller of `WorldCommandBuffer`.
8. Benchmark scheduler overhead.
9. Benchmark serial vs parallel execution at realistic entity counts.
10. Produce a file-level migration map.

Classify current types:

```text
KEEP
SIMPLIFY
REPLACE
DELETE
```

Then propose exact Milestone 1 changes.

STOP.

Do not implement the refactor until the migration map has been reviewed.
