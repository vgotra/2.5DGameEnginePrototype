# ECS and Jobs Design

ECS will use generational entity handles, manually registered component IDs, archetype chunks, column storage, explicit read/write system access, deferred structural command buffers, and a scheduler that runs non-conflicting systems in parallel. Jobs expose completion handles and frame barriers; no subsystem creates unmanaged worker threads implicitly.
