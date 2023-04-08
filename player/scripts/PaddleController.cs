using Godot;
using System;

public partial class PaddleController : CharacterBody2D
{
  public const string PaddleSpritePath = "PaddleSprite";

  [ExportGroup("Paddle Controller Settings")] [ExportSubgroup("Movement")] [Export]
  private Vector2 _movementDirection = Vector2.Right;

  [Export(PropertyHint.Range, "0.1,10000.0,0.1")]
  private float _maxSpeed = 300.0f;

  [Export(PropertyHint.Range, "0.1,10000.0,0.1")]
  private float _acceleration = 25.0f;

  [Export(PropertyHint.Range, "0.1,10000.0,0.1")]
  private float _deceleration = 10.0f;

  [Export(PropertyHint.Range, "0.1,10000.0,0.1")]
  private float _zeroVelocityThreshold = 50.0f;

  [Export(PropertyHint.Range, "0.01,1.0,0.01")]
  private float _friction = 0.75f;

  [ExportSubgroup("Settings")] [Export(PropertyHint.ColorNoAlpha)]
  private Color _paddleColor = new Color(1.0f, 1.0f, 1.0f);

  private bool CanMove = true;
  private float _currentSpeed;

  private Vector2 _moveDirection = Vector2.Zero;
  private Vector2 _spawnPosition = Vector2.Zero;

  private Sprite2D _sprite;

  public override void _Ready()
  {
    _sprite = LoadNode<Sprite2D>(PaddleSpritePath);

    _sprite.Modulate = _paddleColor;
    _spawnPosition = Position;
    _currentSpeed = _maxSpeed;
  }

  private T LoadNode<T>(string path) where T : GodotObject
  {
    T node = GetNode<T>(path);

    if (node == null)
    {
      GD.PrintErr($"Couldn't find object at path: {path}");
    }

    return node;
  }

  public override void _Process(double delta)
  {
    HandleInput();
    FixateFloatHeight();
  }

  private void HandleInput()
  {
    float moveRightStrength = Input.GetActionStrength("MoveRight") - Input.GetActionStrength("MoveLeft");
    float moveUpStrength = Input.GetActionStrength("MoveUp") - Input.GetActionStrength("MoveDown");

    _moveDirection = new Vector2(
      MathF.Sign(moveRightStrength),
      MathF.Sign(moveUpStrength)
    ) * _movementDirection;
  }

  private void FixateFloatHeight()
  {
    if (MathF.Abs(_movementDirection.X) != 0.0f)
    {
      Position = new Vector2(Position.X, _spawnPosition.Y);
    }
    else if (MathF.Abs(_movementDirection.Y) != 0.0f)
    {
      Position = new Vector2(_spawnPosition.X, Position.Y);
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    HandleMovement();
    MoveAndSlide();
  }

  private void HandleMovement()
  {
    if (!CanMove)
    {
      return;
    }

    if (_moveDirection.IsZeroApprox())
    {
      Velocity = SlowDown();
    }
    else
    {
      Velocity = SpeedUp();
    }
  }

  private Vector2 SlowDown()
  {
    float newXVelocity = Velocity.X + (MathF.Sign(Velocity.X) / -1.0f) * _deceleration * _friction;
    float newYVelocity = Velocity.Y + (MathF.Sign(Velocity.Y) / -1.0f) * _deceleration * _friction;

    if (MathF.Abs(newXVelocity) <= _zeroVelocityThreshold)
    {
      newXVelocity = 0.0f;
    }

    if (MathF.Abs(newYVelocity) <= _zeroVelocityThreshold)
    {
      newYVelocity = 0.0f;
    }

    return new Vector2(newXVelocity, newYVelocity);
  }

  private Vector2 SpeedUp()
  {
    float newXVelocity = Velocity.X + _moveDirection.X * _acceleration * _friction;
    float newYVelocity = Velocity.Y + _moveDirection.Y * _acceleration * _friction;

    if (MathF.Abs(newXVelocity) >= _currentSpeed)
    {
      newXVelocity = MathF.Sign(_moveDirection.X) * _currentSpeed;
    }

    if (MathF.Abs(newYVelocity) >= _currentSpeed)
    {
      newYVelocity = MathF.Sign(_moveDirection.Y) * _currentSpeed;
    }

    return new Vector2(newXVelocity, newYVelocity);
  }

  public void Respawn()
  {
    Position = _spawnPosition;
    Velocity = Vector2.Zero;
    CanMove = true;
  }

  public Vector2 GetBounceDirectionForCollision(KinematicCollision2D collision, Vector2 originalDirection)
  {
    Vector2 paddleSize = _sprite.GetRect().Size;
    Vector2 absNormal = new Vector2(
      MathF.Abs(collision.GetNormal().X),
      MathF.Abs(collision.GetNormal().Y)
    );
    Vector2 reverseNormal = new Vector2(
      MathF.Abs(collision.GetNormal().Y),
      MathF.Abs(collision.GetNormal().X)
    );
    Vector2 positionDiff = (collision.GetPosition() - GlobalPosition) / paddleSize.X * 2.0f;
    Vector2 bounceDirection = ((positionDiff * reverseNormal) + (originalDirection * absNormal)).Normalized();
    float paddlePadding = -0.0f;
    
    if (
      MathF.Abs(_movementDirection.X) != 0.0f &&
      MathF.Abs(collision.GetPosition().X - GlobalPosition.X) < paddleSize.X * Scale.X / 2.0f + paddlePadding
    )
    {
      bounceDirection = new Vector2(
        Math.Clamp(bounceDirection.X, -0.65f, 0.65f),
        1.0f * -_movementDirection.X
      );
    }
    else if (
      MathF.Abs(_movementDirection.Y) != 0.0f &&
      MathF.Abs(collision.GetPosition().Y - GlobalPosition.Y) < paddleSize.X * Scale.X / 2.0f + paddlePadding
    ) 
    {
      bounceDirection = new Vector2(
        1.0f * -_movementDirection.Y,
        Math.Clamp(bounceDirection.Y, -0.65f, 0.65f)
      );
    }

    return bounceDirection;
  }

  public Vector2 GetFollowVector()
  {
    return new Vector2(
      MathF.Abs(_movementDirection.X),
      MathF.Abs(_movementDirection.Y)
    );
  }

  public void SetSpeedPercent(float percent)
  {
    if (percent > 0.0f)
    {
      _currentSpeed *= percent;
    }
  }
}
