namespace Engine.Tests;

internal static class TestAssert
{
    public static void True(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException($"Failed: {message}");
    }
}
