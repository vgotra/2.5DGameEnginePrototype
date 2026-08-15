# Sample Plan — Implemented Game Features

Current implementation identifiers are functional (`GameContent`, `GameProgression`, `GameplayContracts`, and `GameplayRuntime`). `Phase1` is retained only for the launch-option compatibility alias.

## Runtime and world foundations

- Typed gameplay IDs for heroes, enemies, NPCs, skills, items, effects, quests, scenes, maps, VFX, sounds, and logical model assets.
- Renderer-neutral `SceneMap`, named markers, `MapLocation`, `WorldMap`, unlock state, and directed travel connections.
- Deferred typed scene spawns through `EntityCommands` and `GameplayCatalog` definitions.
- Value-type attributes, derived stats, inventory, equipment, effects, quests, AI intents, companions, and presentation reactions.

## Gameplay scenario

- `--arpg-sample` configures Village and Goblin Forest scenes and their named markers; `--phase1` remains a compatibility alias.
- Game content definitions include Elder Marcus, blacksmith, goblin warrior/archer/shaman, cleric companion, Goblin Problem, and Goblin Slayer Bow reward.
- Fixed-step progression now accepts the quest, unlocks/travels to Goblin Forest, spawns enemies and companion, records deterministic combat kills, awards loot/gold, and returns to Village to complete the quest.
- Existing normal, `--simulation`, and `--arpg` sample paths remain available.

## glTF source seam

- `ModelHandle`, glTF vertex/index/primitive/node/skin/animation/material contracts are renderer-neutral.
- The offline manifest script validates glTF 2.0 JSON metadata and establishes deterministic source-to-runtime asset IDs.
- glTF JSON/GLB assets decode supported PNG/JPEG images, animation tracks, and skeletal data for build-time baking.
- The deterministic baker samples base-color textures, applies material bake lighting and alpha testing, evaluates animated poses, and emits atlas pixels plus frame UV metadata.
- Cooked atlases upload once through `TextureLibrary`; `CharacterVisual`, `Renderable`, `RenderItem`, and `SpritePacket` preserve logical asset/frame/direction selection. Procedural/PNG fallback remains available.

## Input, movement, and hotbar contracts

- `PlayerCommand` maps to renderer-neutral `CharacterIntent` values for movement, interaction, and skill casting.
- Fixed-capacity ten-slot `Hotbar`, indexed `SkillLoadout`, and `CharacterMovement` components are available.
- Deterministic direct navigation path generation is available for the initial movement seam.
- `BuildGeneratedGameAssets.ps1` packages optional generated manifests and pre-baked atlases during the sample build; absent source assets produce a valid empty manifest and preserve fallback visuals. Generated output uses the neutral `game-bake.json` manifest name.

## Unified input and action sets

- Device-neutral `ActionBinding`, `VirtualInput`, and `ActionSetResolver` contracts support virtual controls and gamepad modifier mappings.
- Standard gamepad actions map to skills 1–4; the secondary modifier maps the same face actions to skills 5–8.
- Virtual input preserves held state while latching and consuming pressed edges through `PlayerCommand`.
- Physical number keys now map to skill slots 1–10, and gameplay progression interaction is routed through `CharacterIntent`.
- `NavigationRuntime` applies fixed-step `CharacterIntent` movement for the ARPG player and Cleric companion through `CharacterMovement`; existing terrain/collision systems remain compatible.
- ARPG primary-click aim now produces move-to requests; player and companion navigation resolves candidate positions through terrain occupancy checks, with deterministic blocked-destination coverage.
- Deterministic grid navigation now routes ARPG player and companion movement around obstacles using stable breadth-first neighbor ordering.
- ARPG player and companion paths are cached between target changes and expose renderer-neutral moving/arrived/blocked navigation results.
- Navigation arrival and blocked states publish bounded `PresentationReaction` records without coupling gameplay to Vulkan.
- Release benchmark verification now supports a bounded `--quick` smoke mode with per-case progress and allocation reporting.

## Remaining work

- Complete a full production glTF art set and broaden asset lifetime management, atlas repacking, and runtime asset eviction.
- Consider a future `World` → `GameWorldContext` public API migration only if the existing engine API can be changed without compatibility cost.
