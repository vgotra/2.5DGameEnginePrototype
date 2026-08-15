Пане Володимир! Yes. I would make the **first Codex implementation plan deliberately game-oriented rather than engine-oriented**. The engine should provide primitives; the game layer should make spawning a Paladin, Goblin Shaman, quest giver, weapon, VFX, companion, etc. almost trivial.

One terminology choice first: I would use **IDs as `readonly record struct` / string constants rather than giant enums**. Enums are fine for tiny closed concepts such as `Team`, but `SkillId`, `ItemId`, `EffectId`, `NpcId`, etc. should be extensible IDs. That prevents the classic `enum` becoming a 700-line dumping ground.

## Codex implementation plan — Phase 1

> Sections marked **Implemented** are summarized in [SamplePlanGameImplemented.md](SamplePlanGameImplemented.md). The Phase 1 label remains historical planning context; current code uses functional engine and gameplay names. Only unmarked/future guidance is an active planning target.

### 1. Establish the gameplay vocabulary — Implemented

Use conventional ARPG terminology:

```text
World
 ├── WorldMap
 ├── SceneManager
 ├── GameState / SaveState
 └── Scene
      ├── SceneMap
      ├── SpawnSystem
      ├── EntityRegistry
      ├── CombatSystem
      ├── EffectSystem
      ├── SkillSystem
      ├── InventorySystem
      ├── QuestSystem
      ├── DialogueSystem
      ├── AI System
      ├── VfxSystem
      └── AudioSystem
```

Important distinction:

```text
World = global game state and collection of scenes
Scene = where gameplay actually happens
SceneMap = playable map/location
WorldMap = high-level navigation between locations
```

So **World must not become another God Object containing gameplay logic**.

---

# 2. Four playable hero archetypes — Implemented

Start with exactly:

```csharp
HeroId.Rogue
HeroId.Paladin
HeroId.Cleric
HeroId.Druid
```

I would call the first class **Rogue**, with an Archer-oriented starting build. That leaves room for melee/poison/trap builds later.

Each hero gets only these primary attributes initially:

```csharp
public struct Attributes
{
    public int Strength;      // STR
    public int Dexterity;     // DEX
    public int Intelligence;  // INT
    public int Vitality;      // VIT
    public int Spirit;        // SPI
}
```

Keep values conceptually around **1–10**.

Do not add Diablo-style:

```text
Strength
Dexterity
Intelligence
Vitality
Willpower
Spirit
Luck
Accuracy
Resolve
Tenacity
...
```

We don't need a spreadsheet simulator.

Derived stats can exist separately:

```csharp
public struct CombatStats
{
    public float MaxHealth;
    public float MaxMana;

    public float AttackPower;
    public float SpellPower;
    public float Armor;

    public float MoveSpeed;
    public float AttackSpeed;

    public float CritChance;
}
```

Derived values should normally be calculated from attributes + equipment + effects.

---

# 3. Entity spawning API — Implemented

This is one of the most important APIs.

The game code should eventually look approximately like this:

```csharp
var paladin = scene.SpawnHero(
    HeroId.Paladin,
    MapLocation.At("VillageSquare"));

var goblin = scene.SpawnEnemy(
    EnemyId.GoblinWarrior,
    MapLocation.At("OldBridge"));

var merchant = scene.SpawnNpc(
    NpcId.VillageBlacksmith,
    MapLocation.At("Blacksmith"));

var questGiver = scene.SpawnNpc(
    NpcId.ElderMarcus,
    MapLocation.At("VillageCenter"));

scene.SpawnItem(
    ItemId.IronSword,
    MapLocation.At("DungeonChest01"));

scene.SpawnVfx(
    VfxId.Teleport,
    MapLocation.At("Portal"));

scene.PlaySound(
    SoundId.PortalOpen,
    MapLocation.At("Portal"));
```

Notice **`MapLocation`**, not leaking renderer-oriented `Vector3 position` everywhere.

Internally:

```csharp
public readonly record struct MapLocation(
    MapId Map,
    float X,
    float Y,
    float Elevation = 0);
```

And named markers:

```csharp
MapLocation.At("VillageSquare")
MapLocation.At("DungeonEntrance")
MapLocation.At("BossArena")
```

The scene/map resolves those markers.

---

# 4. Spawn definitions, not constructors everywhere — Implemented

Do **not** let game code do this:

```csharp
new Goblin(
    hp: 53,
    armor: 7,
    damage: 12,
    speed: 4.2f,
    texture: ...,
    sound: ...);
```

Instead:

```csharp
scene.SpawnEnemy(
    EnemyId.GoblinShaman,
    MapLocation.At("GoblinCamp"));
```

Definition:

```csharp
EnemyDefinition GoblinShaman = new()
{
    Id = EnemyId.GoblinShaman,

    Stats = new()
    {
        Strength = 2,
        Dexterity = 3,
        Intelligence = 6,
        Vitality = 3,
        Spirit = 5
    },

    Skills =
    [
        SkillId.FireBolt,
        SkillId.HealGoblin,
        SkillId.Curse
    ],

    Brain = BrainId.GoblinShaman
};
```

Later this can move to JSON/TOML/YAML/asset database without changing the gameplay API.

---

# 5. Unified gameplay effects — Implemented

This is probably the **single most useful abstraction** for this engine.

Instead of separate bespoke implementations for:

* poison
* burning
* stun
* +STR
* healing
* mana regeneration
* weapon bonuses
* skill buffs
* debuffs
* slow

use:

GameplayEffect

For example:

```csharp
EffectId.Burning
EffectId.Poisoned
EffectId.Blessed
EffectId.Stunned
EffectId.Frozen
EffectId.StrengthBonus
EffectId.HealthRegeneration
```

Then:

```csharp
target.Effects.Apply(
    EffectId.Burning,
    source: caster);
```

A weapon can apply an effect:

```csharp
FireBow.OnHit =
[
    EffectId.Burning
];
```

A skill can apply one:

```csharp
FireArrow.Effects =
[
    EffectId.FireDamage,
    EffectId.Burning
];
```

An item can modify attributes:

```csharp
RingOfStrength.Modifiers =
[
    StatModifier.Add(StatId.Strength, 2)
];
```

This dramatically reduces special-case code.

---

# 6. Combat event pipeline — Implemented

Don't hardcode everything inside `Attack()`.

Use a small pipeline:

```text
Attack
 ↓
Hit
 ↓
Damage
 ↓
Apply Effects
 ↓
Trigger Reactions
 ↓
Death if HP <= 0
```

Example:

```csharp
combat.Hit(attacker, target, weapon);
```

Internally:

```csharp
HitResult result = damage.Calculate(...);

target.Health.ApplyDamage(result.Damage);

effects.ApplyOnHit(attacker, target, weapon);

events.Publish(new HitEvent(
    attacker,
    target,
    result));
```

This enables:

```text
fire sword
poison arrows
life steal
critical-hit VFX
hit sounds
floating damage text
quest counters
AI reactions
```

without putting all those systems into the weapon.

---

# 7. VFX, sound and floating text are gameplay reactions — Implemented

For example data-driven:

```csharp
Fireball =
{
    ImpactVfx = VfxId.FireExplosion,
    CastSound = SoundId.FireballCast,
    ImpactSound = SoundId.FireImpact
};
```

Then gameplay code stays:

```csharp
caster.Cast(SkillId.Fireball, goblin);
```

---

# 8. Skills — maximum 10 equipped — Implemented

I suggest separating:

```text
Known Skills
Skill Levels
Equipped Skill Bar
```

Example:

```csharp
hero.Skills.Learn(SkillId.PowerShot);

hero.Skills.Upgrade(SkillId.PowerShot);

hero.SkillBar.Assign(
    slot: 1,
    SkillId.PowerShot);
```

Maximum:

```csharp
public const int MaxSkillSlots = 10;
```

Example Rogue:

```text
1 Basic Shot
2 Power Shot
3 Multishot
4 Poison Arrow
5 Rain of Arrows
6 Dash
7 Smoke Bomb
8 Trap
9 Eagle Eye
10 Ultimate / specialization
```

But don't implement 40 skills initially.

Implement perhaps **2 skills per class** to prove the architecture.

---

# 9. Equipment + inventory — Implemented

Keep inventory completely UI-independent.

```csharp
Inventory inventory = hero.Inventory;

inventory.Add(ItemId.HealthPotion);
inventory.Add(ItemId.IronSword);
inventory.Add(ItemId.HunterBow);

hero.Equipment.Equip(
    EquipmentSlot.MainHand,
    ItemId.HunterBow);
```

Equipment slots:

```csharp
public static class EquipmentSlot
{
    public const string MainHand = "main-hand";
    public const string OffHand = "off-hand";
    public const string Head = "head";
    public const string Chest = "chest";
    public const string Hands = "hands";
    public const string Feet = "feet";
    public const string Ring1 = "ring-1";
    public const string Ring2 = "ring-2";
    public const string Amulet = "amulet";
}
```

Equipping should produce modifiers:

```text
Base stats
+ Equipment modifiers
+ Passive skill modifiers
+ Temporary effects
---------------------------
Final stats
```

Conceptually:

```csharp
hero.Stats.Get(StatId.Strength);
```

rather than manually modifying `hero.Strength` every time equipment changes.

---

# 10. NPCs — Implemented

Don't create:

```text
MerchantEntity
QuestEntity
DialogueEntity
CompanionEntity
GuardEntity
...
```

Create an NPC composed from capabilities.

```csharp
NpcDefinition Blacksmith = new()
{
    Id = NpcId.Blacksmith,

    Dialogue = DialogueId.Blacksmith,

    Merchant = MerchantId.BlacksmithShop,

    Brain = BrainId.VillageNpc
};
```

Quest giver:

```csharp
NpcDefinition Elder = new()
{
    Id = NpcId.ElderMarcus,

    Dialogue = DialogueId.ElderMarcus,

    Quests =
    [
        QuestId.GoblinProblem
    ]
};
```

Companion:

```csharp
NpcDefinition WolfCompanion = new()
{
    Id = NpcId.WolfCompanion,

    Brain = BrainId.CompanionWolf,

    Team = Team.Player
};
```

Same fundamental entity architecture; different capabilities.

---

# 11. Quests — Implemented

Keep quests extremely simple initially.

```csharp
QuestId.GoblinProblem
```

Definition:

```csharp
new QuestDefinition
{
    Id = QuestId.GoblinProblem,

    Objectives =
    [
        Kill(EnemyId.GoblinWarrior, 10),
        Kill(EnemyId.GoblinShaman, 2)
    ],

    Rewards =
    [
        Gold(100),
        Item(ItemId.GoblinSlayerBow)
    ]
};
```

Later:

```text
Kill
Collect
Talk
Explore
Escort
Defend
UseItem
Interact
```

covers a surprisingly large percentage of ARPG quests.

---

# 12. Replace complicated FSMs with **Intent + Action** — Implemented

I particularly recommend this.

Don't build this monster:

```text
IdleState
WalkingState
RunningState
AttackState
ChaseState
SearchState
FleeState
CastState
HealState
StunnedState
...
```

with hundreds of state transitions.

Instead AI chooses an **Intent**:

```csharp
Idle
MoveTo
Follow
Attack
Cast
Flee
Interact
Guard
Patrol
```

and an `ActionController` executes it.

Example Goblin Shaman brain:

```csharp
public AiIntent Think(AiContext ctx)
{
    var woundedAlly = ctx.FindWoundedAlly();

    if (woundedAlly != null)
        return Cast(SkillId.HealGoblin, woundedAlly);

    if (ctx.Target.IsClose)
        return FleeFrom(ctx.Target);

    if (ctx.CanSeeEnemy)
        return Cast(SkillId.FireBolt, ctx.Target);

    return Patrol();
}
```

That is vastly easier to understand than a conventional giant FSM.

---

# 13. AI companions — Implemented

This architecture naturally supports solo-player companions.

Example:

```csharp
public AiIntent Think(AiContext ctx)
{
    if (ctx.Owner.Health.Percent < 0.30f)
        return Cast(SkillId.Heal, ctx.Owner);

    if (ctx.Owner.HasTarget)
        return Attack(ctx.Owner.Target);

    if (ctx.DistanceToOwner > 5)
        return Follow(ctx.Owner);

    return Guard(ctx.Owner);
}
```

Eventually companion tactics could be configurable:

```text
Aggressive
Defensive
Support
Ranged
StayClose
ProtectPlayer
FocusPlayerTarget
```

This gives the "game friends" idea without requiring sophisticated AI.

---

# 14. Scenes — Implemented

Example API:

```csharp
await game.Scenes.Load(SceneId.TristramVillage);
```

A scene definition:

```csharp
SceneDefinition Village = new()
{
    Id = SceneId.TristramVillage,
    Map = MapId.TristramVillage,

    Spawns =
    [
        Npc(NpcId.Blacksmith, "Blacksmith"),
        Npc(NpcId.ElderMarcus, "VillageCenter"),
        Enemy(EnemyId.GoblinScout, "NorthGate")
    ]
};
```

Changing scenes:

```csharp
game.Scenes.Enter(
    SceneId.GoblinForest,
    EntryPointId.SouthEntrance);
```

Persistent global information belongs in `World`:

```text
completed quests
unlocked locations
player party
global flags
time/day if needed
world progression
```

Transient/local things belong in `Scene`:

```text
enemies
NPC instances
projectiles
VFX
local triggers
loot on ground
doors
local AI
```

This boundary will matter enormously later.

---

# 15. World map — Implemented

Keep it as a graph, not another giant scene.

```text
Village
  |
Goblin Forest
  |
Old Ruins
 /       \
Crypt   Demon Gate
          |
       Hell Rift
```

API:

```csharp
world.Map.Unlock(LocationId.GoblinForest);

world.Map.TravelTo(LocationId.GoblinForest);
```

Definition:

```csharp
WorldLocation GoblinForest = new()
{
    Id = LocationId.GoblinForest,
    Scene = SceneId.GoblinForest
};
```

Thus terminology stays clear:

**World → World Map → Location → Scene → Scene Map → Map Location.**

---

# 16. Suggested ID pattern

Rather than enums everywhere:

```csharp
public readonly record struct SkillId(string Value);

public static class Skills
{
    public static readonly SkillId Fireball = new("fireball");
    public static readonly SkillId Heal = new("heal");
    public static readonly SkillId PowerShot = new("power-shot");
}
```

Or slightly nicer:

```csharp
public static class SkillId
{
    public const string Fireball = "fireball";
    public const string Heal = "heal";
    public const string PowerShot = "power-shot";
}
```

I prefer **typed IDs** for engine-facing code because accidentally passing an `ItemId` where `SkillId` is expected becomes impossible.

Use enums only for truly closed concepts:

```csharp
enum Team
{
    Neutral,
    Player,
    Enemy
}
```

Even these can later become IDs if modding demands it.

---

# 17. One target gameplay example Codex should make work

This should be our acceptance test.

```csharp
var rogue = scene.SpawnHero(
    HeroId.Rogue,
    MapLocation.At("ForestEntrance"));

rogue.Inventory.Add(ItemId.FireBow);

rogue.Equipment.Equip(
    EquipmentSlot.MainHand,
    ItemId.FireBow);

rogue.Skills.Learn(SkillId.PowerShot);
rogue.Skills.Learn(SkillId.PoisonArrow);

var shaman = scene.SpawnEnemy(
    EnemyId.GoblinShaman,
    MapLocation.At("GoblinCamp"));

var warrior = scene.SpawnEnemy(
    EnemyId.GoblinWarrior,
    MapLocation.At("GoblinCamp"));

rogue.Cast(
    SkillId.PoisonArrow,
    shaman);
```

From that single call:

```text
PoisonArrow
     ↓
Attack/Projectile
     ↓
Hit GoblinShaman
     ↓
Weapon damage
     ↓
Poison effect
     ↓
FireBow burning effect
     ↓
Impact VFX
     ↓
Impact sound
     ↓
Floating damage text
     ↓
Shaman AI reacts
     ↓
Quest kill tracking eventually reacts
```

That is the architecture I would optimize for.

---

# 18. What Codex should NOT build yet

This is critical.

**Do not implement yet:**

```text
Behavior Trees
GOAP
complex FSM framework
network replication
editor
visual scripting
reflection-heavy DI framework
generic scripting VM
massive archetype hierarchy
complex animation graph
100+ stats
procedural world generation
generic modding system
serialization framework for everything
```

All of those are attractive ways to accidentally spend six months building an engine instead of a game.

---

# 19. Games/systems worth studying

For design inspiration, I'd specifically examine:

* **Diablo II** — extremely readable item/skill/stat foundations.
* **Diablo III** — excellent skill readability, combat feedback, VFX and simplified character systems.
* **Grim Dawn** — useful reference for ARPG stats, effects, factions and world structure.
* **Torchlight II** — particularly relevant because it demonstrates how much ARPG gameplay can be achieved without absurd system complexity.
* **Anima ARPG** — useful mobile-oriented reference for simplifying Diablo-like mechanics.
* **Hades** — not a Diablo clone, but excellent for compact combat architecture, effects, readable feedback and NPC interaction.
* **Dragon Age: Origins** — worth examining for companion AI/tactics concepts rather than combat architecture.

For our engine, I would steal the **simplicity of Torchlight/Anima**, the **combat readability of Diablo III**, and a radically simplified version of **Dragon Age companion tactics**.

## Refactoring strategy

Most importantly, tell Codex explicitly:

> **Phase 1 is a vertical gameplay prototype, not the final engine architecture. Prefer simple concrete implementations and clean boundaries over speculative abstractions. Do not generalize a subsystem until at least two or three real gameplay features require the generalization.**

Then after we can run:

**Rogue + Paladin + Cleric + Druid → village → merchant → quest → forest → goblins → companion → skills → loot → equipment → boss → return quest**

we perform **Engine Refactor #1**.

At that point we'll know which abstractions are actually real.

I would make the next Codex task a **small playable vertical slice**: *Village → accept Goblin quest → buy/equip bow → enter Goblin Forest → fight Warrior + Archer + Shaman → companion Cleric assists → loot weapon → equip it → stats change → return to village*. That one slice will force nearly every important engine API above to prove itself.

Input → Player Command → Character Controller layer to the Codex plan.

# 20. Input: keyboard, mouse, gamepad, virtual controls — Implemented

> Implemented baseline moved to `SamplePlanGameImplemented.md`, including runtime keyboard and mouse rebinding, virtual input, and gamepad modifier action sets.

Gameplay code should never check physical keys:

// BAD
```csharp
if (keyboard.IsDown(Keys.W))
    player.Move(...);
```

Instead, use game actions:

```csharp
InputAction.Move
InputAction.PrimaryAttack
InputAction.SecondaryAttack


InputAction.Skill1
InputAction.Skill2
// ...
InputAction.Skill10


InputAction.Interact
InputAction.Pickup
InputAction.Inventory
InputAction.Character
InputAction.WorldMap


InputAction.Dodge
InputAction.UsePotion
```

Then bindings decide how an action is activated.

Keyboard/mouse

```text
Move              WASD / mouse click
PrimaryAttack     Left Mouse
SecondaryAttack   Right Mouse
Skill1..Skill10   1..0
Interact          E
Pickup            F
Inventory         I
Character         C
WorldMap          M
Potion            Q
```

```text
Gamepad
Move              Left Stick
Aim/Target        Right Stick
PrimaryAttack     RT
SecondaryAttack   LT
Skill1            A
Skill2            X
Skill3            Y
Skill4            B
Interact          RB
Potion            D-Pad Up
Inventory         Menu
```

And importantly: bindings must be remappable.

# 21. One unified API — Implemented

The character controller shouldn't care whether input came from keyboard, gamepad, Steam Deck-like controls, or virtual/mobile joystick.

```csharp
public interface IPlayerInput
{
    Vector2 Move { get; }
    Vector2 Aim { get; }


    bool Pressed(InputAction action);
    bool Held(InputAction action);
    bool Released(InputAction action);
}
```

Implementations:

```text
KeyboardMouseInput
GamepadInput
VirtualInput
AiInput
```

That last one becomes surprisingly useful.

A human-controlled Paladin:

```text
Gamepad
   ↓
GamepadInput
   ↓
CharacterController
   ↓
Paladin

An AI companion:

CompanionBrain
   ↓
AiInput / CharacterCommands
   ↓
CharacterController
   ↓
Cleric
```

So humans and AI can reuse much of the same movement/combat machinery.

# 22. Movement must be extremely simple for game code — Implemented

For a 2.5D isometric ARPG, game logic shouldn't deal with Vulkan, transforms, physics internals, etc.

Conceptually:

```csharp
player.Move(direction);
```

or:

```csharp
player.MoveTo(mapLocation);
```

Two movement modes can coexist.

Direct movement

Keyboard/gamepad:

```csharp
hero.Move(input.Move);
```

Click-to-move

Mouse:

```csharp
hero.MoveTo(mouse.MapLocation);
```

That gives us both modern controller movement and classic Diablo-style mouse movement.

# 23. Use MapLocation, not rendering coordinates — Implemented

Continue our previous rule:
```csharp
hero.Location
```

rather than exposing:

```csharp
hero.Transform.Position
```

Game code:

```csharp
hero.MoveTo(
    MapLocation.At("Blacksmith"));
```

AI:

```csharp
companion.Follow(player);
```

Quest:

if (player.Location.IsInside(AreaId.OldRuins))
    ...

Engine internals can still use vectors, matrices, Vulkan coordinates, navigation coordinates, etc.

# 24. Character movement component — Implemented

I'd keep the initial controller tiny:

```csharp
public sealed class CharacterMotor
{
    public MapLocation Location { get; }


    public float MoveSpeed { get; }


    public void Move(Vector2 direction);


    public void MoveTo(MapLocation destination);


    public void Stop();
}
```

Later:

```csharp
motor.Move(direction);
motor.MoveTo(location);
motor.Follow(entity);
motor.Stop();
```

Don't build a giant CharacterControllerBase<TMovementStrategy...> hierarchy.

# 25. Navigation — Implemented

For enemies/NPCs/click-to-move, hide navigation behind:

navigation.MoveTo(entity, destination);

Example:

```csharp
goblin.MoveTo(player.Location);
```

Internally this might eventually become:

```text
Current MapLocation
       ↓
Navigation query
       ↓
Path
       ↓
Steering
       ↓
CharacterMotor
```

But Goblin AI does not care.

```csharp
if (ctx.CanAttackTarget)
    return Attack(ctx.Target);

return MoveTo(ctx.Target.Location);
```

Very readable.

# 26. Shortcuts / hotbar — Implemented

> Implemented baseline moved to `SamplePlanGameImplemented.md`: fixed-capacity ten-slot hotbar, indexed skill loadout lookup, and learned-skill validation are available.

We should explicitly create a Hotbar, separate from keyboard bindings.

This distinction matters.

```csharp
hero.Hotbar.Assign(0, SkillId.BasicShot);
hero.Hotbar.Assign(1, SkillId.PowerShot);
hero.Hotbar.Assign(2, SkillId.PoisonArrow);
hero.Hotbar.Assign(3, SkillId.Multishot);
...
hero.Hotbar.Assign(8, ItemId.HealthPotion);
```

The hotbar says:

Slot 2 contains Poison Arrow.

Input binding says:

Keyboard 2 activates Hotbar Slot 2.

Therefore:

```text
Keyboard 2 ─────┐
                │
Gamepad X ──────┼──→ Hotbar Slot 2 ─→ Poison Arrow
                │
Virtual Button ─┘
```

This is much cleaner.

# 27. Gamepad should not require 10 physical skill buttons — Implemented

Ten skills create a controller problem.

I suggest action sets / modifier triggers.

For example:

```text
A / X / Y / B
```

are four normal skills.

Hold LT:

```text
LT + A
LT + X
LT + Y
LT + B
```

four additional skills.

Then:

```text
RT = Primary Attack
RB = Interact
```

That's already enough for an ARPG without making the controller feel like an aircraft cockpit.

We can still allow up to 10 skills, while a particular control scheme decides how they are exposed.

# 28. Virtual controls — Implemented

Design this now even if we don't implement mobile UI yet.

Virtual joystick:

```csharp
virtualInput.SetMove(stick.Direction);
```

Virtual skill button:

```csharp
virtualInput.Press(InputAction.Skill1);
```

This means future touch controls don't modify gameplay code at all.

```text
┌──────────────────────────────┐
│                              │
│          GAME WORLD          │
│                              │
│                              │
│   ◯                      (A) │
│ virtual               Skill │
│ joystick             (B)(C) │
└──────────────────────────────┘
```

Same concept could support accessibility controllers or unusual devices later.

# 29. Separate Input from Intent — Implemented

I'd actually refine our earlier architecture to:

PHYSICAL INPUT

```text
Keyboard / Mouse / Gamepad / Touch
             ↓
         InputAction
             ↓
       PlayerController
             ↓
       CharacterIntent
             ↓
      Character Actions
             ↓
        Game Systems
```

AI skips physical input:

```text
AI Brain
   ↓
CharacterIntent
   ↓
Character Actions
   ↓
Game Systems
```

For example:

```csharp
CharacterIntent.Move(direction);

CharacterIntent.MoveTo(location);

CharacterIntent.Attack(target);

CharacterIntent.Cast(
    SkillId.Fireball,
    target);

CharacterIntent.Interact(npc);
```

This becomes our simplified replacement for a giant state machine.

# 30. The resulting architecture — Implemented

I would now give Codex this target:

```text
                    ┌─ Keyboard/Mouse
                    ├─ Gamepad
                    └─ Virtual Controls
                           │
                           ▼
                      Input Actions
                           │
                           ▼
                    Player Controller
                           │
                           ▼
                    Character Intent
                           │
         ┌─────────────────┼─────────────────┐
         ▼                 ▼                 ▼
      Movement           Combat          Interaction
         │                 │                 │
         ▼                 ▼                 ▼
   CharacterMotor     Skill/Effect       NPC / Loot
                          System           / Quest
```

```text
AI Brain ─────────────→ Character Intent
```

This is a very good simplification because Rogue, Paladin, Cleric, Druid, Goblin, Demon and AI companion ultimately issue the same small vocabulary of character intentions.

A Paladin doesn't need a PaladinMovementSystem. A Goblin doesn't need GoblinMovementSystem. A gamepad doesn't need GamepadCharacterMovement.

They all converge on:

```text
Move(...)
MoveTo(...)
Attack(...)
Cast(...)
Interact(...)
UseItem(...)
Stop()
```
