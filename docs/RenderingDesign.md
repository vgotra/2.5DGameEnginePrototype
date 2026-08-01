# Rendering Design

Rendering is split into backend-neutral extraction and a Vortice.Vulkan implementation. The initial renderer uses per-frame synchronization, upload arenas, sprite batches grouped by texture/material/blend state, texture atlases, orthographic isometric projection, and explicit GPU resource lifetime tracking.
