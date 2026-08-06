# Vulkan & Native Interop

- BIND Vulkan through the Vortice.Vulkan managed package. USE `[LibraryImport]` ONLY for OS/native-C P/Invoke (OS windowing interop such as SDL3-CS's bindings, native C libraries such as JoltC). NEVER `[DllImport]`.
- USE only blittable types. PASS `void*` or `int*` — never managed arrays.
- ENFORCE `[SuppressGCTransition]` for fast (<1µs) unmanaged calls; NEVER on calls that take locks, call back into managed code, or run long.
- ALWAYS return and evaluate `VkResult`. FORBID C# exceptions for native errors.
- ENFORCE `[StructLayout(LayoutKind.Sequential)]` or `Explicit` for native interop structs.
- USE debug-only validation layers, disabled in Release, for zero shipping cost.
