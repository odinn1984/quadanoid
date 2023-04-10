using Godot;
using System;

public partial class BallController : CharacterBody2D
{
  public const string BallSpritePathName = "BallSprite";
  public const string BallTriggerAreaName = "BallTriggerArea";
  public const string PaddleTriggerAreaName = "PaddleTriggerArea";
  public const string TrailName = "Trail";
  public const string AudioStreamsPath = "AudioStreams";

  [ExportGroup("Ball Controller Settings")]
  [Export(PropertyHint.ColorNoAlpha)]
  private Color _ballColor = new Color(1.0f, 1.0f, 1.0f);

  [ExportSubgroup("Movement")]
  [Export]
  private bool _launchOnStart = false;
  
  [Export]
  private float _maxSpeed = 200.0f;

  private bool CanMove { get; set; }

  private uint _trailLength = 100;
  private uint _nextAudioStream = 0;
  private float _currentSpeed;

  private CharacterBody2D _followPaddle = null;

  private Vector2 _direction = Vector2.Zero;
  private Vector2 _spawPosition;

  private Sprite2D _sprite;
  private Line2D _trail;
  private Control _audioStreams;
  private Godot.Collections.Array<Node> _audioStreamsPlayer;

  public override void _Ready()
  {
    CanMove = _launchOnStart;
    _currentSpeed = _maxSpeed;

    SetInitialDirection();

    _spawPosition = Position;

    _sprite = LoadNode<Sprite2D>(BallSpritePathName);
    _sprite.Modulate = _ballColor;

    Area2D triggerArea = LoadNode<Area2D>(BallTriggerAreaName);
    triggerArea.BodyEntered += OnTriggerEntered;
    triggerArea.BodyExited += OnTriggerExited;

    if (triggerArea.GetOverlappingBodies().Count > 0)
    {
      foreach (Node2D body in triggerArea.GetOverlappingBodies())
      {
        OnTriggerEntered(body);
      }
    }

    Area2D paddleTriggerArea = LoadNode<Area2D>(PaddleTriggerAreaName);
    paddleTriggerArea.BodyEntered += OnPaddleEntered;
    paddleTriggerArea.BodyExited += OnPaddleExited;

    if (paddleTriggerArea.GetOverlappingBodies().Count > 0)
    {
      foreach (Node2D body in paddleTriggerArea.GetOverlappingBodies())
      {
        OnPaddleEntered(body);
      }
    }

    _trail = LoadNode<Line2D>(TrailName);
    _audioStreams = LoadNode<Control>(AudioStreamsPath);
    _audioStreamsPlayer = _audioStreams.GetChildren();
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

  public void OnTriggerEntered(Node2D body)
  {
  }

  public void OnTriggerExited(Node2D body)
  {
    if (body.IsInGroup("Bricks"))
    {
      Random random = new Random();
      AudioStreamPlayer2D stream = GetNextAudioStream();

      stream.PitchScale = Math.Clamp(random.NextSingle(), 0.75f, 1.25f);
      stream.Play();
      body.Call("Hit");
    }
  }

  public void OnPaddleEntered(Node2D body)
  {
    if (body.IsInGroup("Paddles"))
    {
      if (CanMove)
      {
        Random random = new Random();
        AudioStreamPlayer2D stream = GetNextAudioStream();

        stream.PitchScale = Math.Clamp(random.NextSingle(), 0.5f, 0.55f);
        stream.Play();
      }
      else
      {
        _followPaddle = body as CharacterBody2D;
      }
    }
  }

  private AudioStreamPlayer2D GetNextAudioStream()
  {
    uint currentStream = _nextAudioStream;

    _nextAudioStream = (_nextAudioStream + 1) % (uint)_audioStreamsPlayer.Count;

    return _audioStreamsPlayer[(int)currentStream] as AudioStreamPlayer2D;
  }

  public void OnPaddleExited(Node2D body)
  {
    _followPaddle = null;
  }

  public override void _Process(double delta)
  {
    if (_followPaddle != null)
    {
      Vector2 followVector = _followPaddle.Call("GetFollowVector").AsVector2();
      Vector2 invertedFollowVector = new Vector2(followVector.Y, followVector.X);

      GlobalPosition = (_followPaddle.GlobalPosition * followVector) + (GlobalPosition * invertedFollowVector);
    }

    if (Velocity.Length() > 0.0f)
    {
      _trail.GlobalPosition = Vector2.Zero;
      _trail.Rotation = 0.0f;

      _trail.AddPoint(GlobalPosition);

      while (_trail.GetPointCount() > _trailLength)
      {
        _trail.RemovePoint(0);
      }
    }
  }

  public override void _PhysicsProcess(double delta)
	{
    if (!CanMove)
    {
      return;
    }

    Velocity = _currentSpeed *_direction;

    KinematicCollision2D collision = MoveAndCollide(Velocity * (float)delta);

    ApplyBounceIfColliding(collision);
  }

  private void ApplyBounceIfColliding(KinematicCollision2D collision)
  {
    if (collision != null)
    {
      _direction = _direction.Bounce(collision.GetNormal()).Normalized();

      CharacterBody2D collisionBody = collision.GetCollider() as CharacterBody2D;

      if (collisionBody != null && collisionBody.IsInGroup("Paddles"))
      {
        _direction = collisionBody.Call("GetBounceDirectionForCollision", collision, _direction).AsVector2();
      }
    }
  }

  public void Respawn()
  {
    while (_trail.GetPointCount() > 0)
    {
      _trail.RemovePoint(0);
    }

    Position = _spawPosition;
    Velocity = Vector2.Zero;

    CanMove = _launchOnStart;
    SetInitialDirection();
  }

  public void SetSpeedPercent(float percent)
  {
    if (percent > 0.0f)
    {
      _currentSpeed *= percent;
    }
  }

  private void SetInitialDirection()
  {
    Random random = new Random();

    do
    {
      _direction = new Vector2(Math.Clamp(random.NextSingle() - 0.5f, -0.25f, 0.25f), -1.0f);
    } while (MathF.Abs(_direction.X) <= 0.1f);
  }
}
