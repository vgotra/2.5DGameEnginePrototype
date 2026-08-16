namespace Engine.Rendering;

public enum PresentMode
{
    Mailbox,
    Fifo
}

public readonly record struct PresentationDiagnostics(
    PresentMode RequestedMode,
    PresentMode SelectedMode,
    bool UsedFallback,
    uint SwapchainImageCount);

internal interface IPresentationDiagnostics
{
    PresentationDiagnostics Presentation { get; }
}
