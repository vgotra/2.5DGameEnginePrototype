# Test

```powershell
dotnet run --project tests\Engine.Tests\Engine.Tests.csproj
```

- A plain console app, **NOT** a test framework — do not use `dotnet test`.
- Success = the app prints `Smoke tests passed`.
- Coverage: iso and flat camera centering (800x600 plus 1920x1080), flat-mode extraction count, box shape, cartesian camera mapping, ECS entity recycle/purge, two-store purge on `Destroy`, tile-map collision sliding, `FrameTimer` cap pacing, `GameClock` fixed-step/clamp, and JobSystem drain under 4 and 8 workers.
