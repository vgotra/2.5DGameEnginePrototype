# Verify

After any change, run in order:

1. **Build** with 0 errors (warnings don't matter): `dotnet build Engine.slnx --nologo`.
2. **Test**: `dotnet run --project tests\Engine.Tests\Engine.Tests.csproj` prints `Smoke tests passed`.
3. **Run** the sample, including any flag combos you touched, to confirm rendering and window behavior (flags, fullscreen toggle, resize, `Escape` exit).
