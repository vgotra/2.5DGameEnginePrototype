# Animations (2.5D Isometric)

- STORE animation state (atlas ID, frame index, timer) as flat `struct` components.
- BATCH animation updates via SIMD and parallel jobs.
- SHIFT frame calculation logic to the GPU where possible (passing sprite indices/UV offsets via instanced rendering or compute shaders).
