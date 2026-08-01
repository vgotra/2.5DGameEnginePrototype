using System.Diagnostics;

namespace Engine.Core;

public static class DebugMetrics
{
    [Conditional("DEBUG")]
    public static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
