# Animations (2D)

- STORE animation state (atlas ID, frame index, timer) as flat `struct` components.
- BATCH animation updates via SIMD and parallel jobs.
- SHIFT frame calculation logic to the GPU where possible (passing sprite indices/UV offsets via instanced quads or compute shaders).
- RENDER 2D animation frames through the box/quad path (`--2d` mode) using the same `SpritePacket` contract as isometric diamonds.
