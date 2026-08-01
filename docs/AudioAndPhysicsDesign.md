# Audio and Physics Design

Audio and physics are engine-owned contracts with replaceable adapters. Audio commands are submitted through bounded queues and callbacks do not invoke gameplay. Physics synchronization occurs in explicit simulation stages; Jolt types remain isolated inside the Jolt adapter.
