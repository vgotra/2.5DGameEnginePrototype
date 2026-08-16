using System.Numerics;
using Engine.App;
using Engine.Core;
using SparseWorld = Engine.Ecs.Sparse.World;
using Engine.Ecs.Sparse;
using Engine.Platform;
using IsometricSandbox.Game.Gameplay.Components;

namespace IsometricSandbox.Game.Gameplay.Systems;

public sealed class PlayerMoveSystem
    : ISystem
{
    private readonly TerrainSurface _map;
    private readonly IInputState? _legacyInput;
    private bool _jumpRequested;
    private PlayerCommand _command;
    private bool _hasCommand;

    public PlayerMoveSystem(TerrainSurface map, IInputState input)
    {
        _map = map;
        _legacyInput = input;
    }

    public PlayerMoveSystem(TerrainSurface map)
    {
        _map = map;
    }

    public Entity Player { get; set; }

    public void CaptureInput()
    {
        if (_legacyInput is not null && _legacyInput.WasPressed(GameKey.Space)) _jumpRequested = true;
    }

    public void SetCommand(in PlayerCommand command)
    {
        _command = command;
        _jumpRequested |= command.IsPressed(InputAction.Dodge);
        _hasCommand = true;
    }

    public void Update(SparseWorld world, float deltaSeconds)
    {
        Vector2 direction = _hasCommand ? _command.Move : ReadLegacyMovement();
        bool jumpRequested = _jumpRequested || (_hasCommand && _command.IsPressed(InputAction.Dodge));
        _jumpRequested = false;
        _hasCommand = false;
        ref Position position = ref world.Get<Position>(Player);
        ref Velocity velocity = ref world.Get<Velocity>(Player);
        ref PlayerState state = ref world.Get<PlayerState>(Player);
        PlayerMoveBody body = new()
        {
            Map = _map,
            Direction = direction,
            JumpRequested = jumpRequested,
            DeltaSeconds = deltaSeconds,
        };
        PlayerMoveBody.Execute(ref body, Player, ref position, ref velocity, ref state);
    }

    private Vector2 ReadLegacyMovement()
    {
        if (_legacyInput is null) return Vector2.Zero;
        float right = (_legacyInput.IsDown(GameKey.Right) ? 1 : 0) - (_legacyInput.IsDown(GameKey.Left) ? 1 : 0);
        float down = (_legacyInput.IsDown(GameKey.Down) ? 1 : 0) - (_legacyInput.IsDown(GameKey.Up) ? 1 : 0);
        return new Vector2(right, down);
    }
}
