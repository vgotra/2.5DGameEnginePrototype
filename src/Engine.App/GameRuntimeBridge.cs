namespace Engine.App;

internal interface IGameRuntimeBridge
{
    void ApplyCommands();
    void RunFixedStep(float deltaSeconds);
    void ExtractPresentation(float interpolationAlpha);
    void ConfigurePresentation(Action<float> extractor);
}

internal sealed class WorldRuntimeBridge(World world) : IGameRuntimeBridge
{
    private Action<float>? _presentationExtractor;

    public void ApplyCommands() => world.ApplyCommands();

    public bool IsAlive(Engine.Ecs.Sparse.Entity entity) => world.IsEntityAlive(entity);

    public void RunFixedStep(float deltaSeconds) { }

    public void ExtractPresentation(float interpolationAlpha) => _presentationExtractor?.Invoke(interpolationAlpha);

    public void ConfigurePresentation(Action<float> extractor)
        => _presentationExtractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
}
