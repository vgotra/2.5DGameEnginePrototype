using System.Numerics;

namespace IsometricSandbox.Game;

// The archer: position, facing, the two-tile jump, and the queued mouse
// shot. Movement/jump math comes from MovementSystem; aiming freshens the
// camera first so the target matches the player's position at click time.
public sealed class Player
{
    private Vector2 _jumpStart;
    private Vector2 _jumpTarget;
    private float _jumpTime;
    private Vector2 _aimTarget;
    private bool _pendingShot;

    public Vector2 Position { get; private set; }
    public Vector2 Facing { get; private set; }

    public Player(Vector2 start)
    {
        Position = start;
        Facing = new Vector2(0, 1);
        _jumpStart = start;
        _jumpTarget = start;
        _jumpTime = SampleConfig.JumpDuration;
        _aimTarget = start;
    }

    // True while a jump is in progress; the jump is eased in JumpHeight.
    public bool IsJumping => _jumpTime < SampleConfig.JumpDuration;

    // Render lift in screen pixels: rises to a peak mid-jump, then lands.
    public float JumpHeight
    {
        get
        {
            float progress = Math.Clamp(_jumpTime / SampleConfig.JumpDuration, 0, 1);
            return progress >= 1 ? 0 : MathF.Sin(progress * MathF.PI) * SampleConfig.JumpHeight;
        }
    }

    public void Reset(Vector2 start)
    {
        Position = start;
        Facing = new Vector2(0, 1);
        _jumpStart = start;
        _jumpTarget = start;
        _jumpTime = SampleConfig.JumpDuration;
        _aimTarget = start;
        _pendingShot = false;
    }

    // One fixed simulation step: set facing, start a jump when requested,
    // then ease the jump or walk. `deltaSeconds` is the fixed step size.
    public void Step(TileMap map, Vector2 direction, bool jumpRequested, float deltaSeconds)
    {
        if (direction.LengthSquared() > 0) Facing = Vector2.Normalize(direction);
        if (jumpRequested && !IsJumping) TryStartJump(map);
        if (IsJumping)
        {
            _jumpTime = Math.Min(SampleConfig.JumpDuration, _jumpTime + deltaSeconds);
            Position = Vector2.Lerp(_jumpStart, _jumpTarget, _jumpTime / SampleConfig.JumpDuration);
        }
        else
        {
            Position = MovementSystem.Move(map, Position, direction, SampleConfig.PlayerSpeed, SampleConfig.PlayerRadius, deltaSeconds);
        }
    }

    // Left click: aim at the cursor in world space. The camera is freshened
    // first so the aim matches the player's current position even after
    // frame hitches. The shot is queued and consumed on the next fixed step.
    public void AimAt(IsometricCamera camera, Vector2 mouseScreen, TileMap map)
    {
        camera.Follow(Position, map);
        _aimTarget = camera.ScreenToWorld(mouseScreen, map);
        _pendingShot = true;
    }

    public bool ConsumePendingShot(out Vector2 target)
    {
        target = _aimTarget;
        if (!_pendingShot) return false;
        _pendingShot = false;
        return true;
    }

    private void TryStartJump(TileMap map)
    {
        Vector2 candidate = Position + Facing * 2f;
        if (!map.CanOccupy(candidate, SampleConfig.PlayerRadius)) return;
        _jumpStart = Position;
        _jumpTarget = candidate;
        _jumpTime = 0;
    }
}
