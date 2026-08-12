---
name: audio
description: Applies audio conventions: clip assets via handles, pooled voices, volume/panning/spatial mixing, worker-thread loading, and explicit ownership. Use when writing or reviewing audio, sound effect, or music code in a game engine. Do not use for asset I/O in general (see assets-io) or resource pooling in general (see object-pooling).
---

# Audio

## Apply
- Substitute every `<...>` placeholder with this repo's actual names before applying. The placeholder glossary is `.agents/skills/README.md`.

## Rules
- LOAD clips through the asset system as handles/IDs, never raw pointers in gameplay logic. SEE `assets-io`.
- POOL voices at fixed capacity; acquire a voice per play, release when done. SEE `object-pooling`.
- APPLY volume, panning, and spatial attenuation with deterministic math; no allocation per play.
- LOAD/decode on worker threads; submit plays on the main thread, never block on I/O in the loop. SEE `job-system`.
- ENFORCE single ownership of each clip and voice; track active voices explicitly.
- KEEP audio trigger points deterministic in the fixed step for reproducible behavior. SEE `determinism`.
