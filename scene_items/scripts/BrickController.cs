using System;
using System.Threading.Tasks;
using Godot;

public partial class BrickController : StaticBody2D
{
  [Export]
  private Sprite2D _sprite;

  private uint HitNumber { get; set; } = 1;

  private Color _color = new Color(1.0f, 1.0f, 1.0f);

  private GpuParticles2D _explosion;
  private GpuParticles2D _impact;

  public override void _Ready()
  {
    if (_sprite == null)
    {
      GD.PrintErr("Please make sure you have set the Sprite attribute");
    }

    _explosion = GetNode<GpuParticles2D>("Explosion");
    _impact = GetNode<GpuParticles2D>("Impact");
  }

  public override void _Process(double delta)
  {
    Color[] brickColors =
    {
      new Color("#FFDEB9"),
      new Color("#FE6244"),
      new Color("#FC2947")
    };

    _sprite.Modulate = brickColors[HitNumber]; 
  }

  async public void Hit()
  {
    if (HitNumber > 0)
    {
      HitNumber--;
      _impact.Emitting = true;
      return;
    }

    Name = $"{Name}_Destroyed";

    CollisionLayer = 0;
    _explosion.Emitting = true;

    await Task.Delay(TimeSpan.FromMilliseconds(500));

    _sprite.Visible = false;

    await Task.Delay(TimeSpan.FromMilliseconds(1500));

    QueueFree();
  }

  public void SetPositionOnGrid(Vector2 gridPosition, Vector2 cell, Vector2 offset, Vector2 brickScale)
  {
    Vector2 brickSize = GetBrickSize() * brickScale;
    
    Position = new Vector2(
      gridPosition.X + ((brickSize.X + offset.X) * cell.X),
      gridPosition.Y + ((brickSize.Y + offset.Y) * cell.Y)
    );
  }

  public Vector2 GetBrickSize()
  {
    return _sprite.Texture.GetSize() * Scale;
  }
}
