# Assets, Textures & I/O

- USE asynchronous background threads for all I/O operations and asset decoding.
- USE unmanaged memory for image decoding (e.g., native `stb_image`). FORBID loading assets into managed `byte[]`.
- USE Asset IDs or Handles (structs). FORBID passing raw resource pointers directly to gameplay logic.
- ENFORCE texture atlases and bindless textures in Vulkan to minimize state changes and pipeline binds (crucial for 2.5D isometric rendering).
