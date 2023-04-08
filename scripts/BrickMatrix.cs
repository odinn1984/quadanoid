using System;
using Godot;

public partial class BrickMatrix : Control
{
  [Export(PropertyHint.File)]
  private string _brickPath;

  [Export]
  private Vector2 _gridItemOffset = Vector2.Zero;

  [Export]
  private Node2D _brickContainer;

  private Vector2I _gridSize = new Vector2I(1, 1);
  private Vector2 _gridSizeOverflow = Vector2.Zero;
  private Vector2 _brickSize = Vector2.Zero;

  private BrickController[,] _brickMatrix;

  public override void _Ready()
  {
    if (_brickContainer == null)
    {
      GD.PrintErr("Please make sure you have set the Brick Container attribute");
    }

    InitializeMatrixData();
    CreateBricks();

    _brickContainer.Position += _gridSizeOverflow / 2.0f;
  }

  private void InitializeMatrixData()
  {
    PackedScene brickResource = ResourceLoader.Load<PackedScene>(_brickPath);
    BrickController metaDataBrick = brickResource.Instantiate<BrickController>();

    _brickSize = metaDataBrick.GetBrickSize();
    _gridSize = new Vector2I(
      (int)Mathf.Floor(GetRect().Size.X / _brickSize.X),
      (int)Mathf.Floor(GetRect().Size.Y / _brickSize.Y)
    );

    Vector2 _marginOverflow = _gridItemOffset * _gridSize - _gridItemOffset;

    _gridSizeOverflow = GetRect().Size - (_gridSize * _brickSize + _marginOverflow);
    
    metaDataBrick.QueueFree();
  }

  public void CreateBricks()
  {
    PackedScene brickResource = ResourceLoader.Load<PackedScene>(_brickPath);

    _brickMatrix = new BrickController[_gridSize.X, _gridSize.Y];

    for (int h = 0; h < _gridSize.Y; h++)
    {
      for (int w = 0; w < _gridSize.X; w++)
      {
        BrickController brick = brickResource.Instantiate<BrickController>();

        _brickMatrix[w, h] = brick;

        brick.Call(
          "SetPositionOnGrid",
          Position,
          new Vector2(w, h),
          _gridItemOffset,
          new Vector2(1.0f, 1.0f)
        );

        brick.Renamed += OnBrickDestroyed;

        Random random = new Random();
        uint numberOfHits = (uint)random.Next(0, 3);

        brick.Set("HitNumber", h == Math.Floor(_gridSize.Y / 2.0f) ? -1 : numberOfHits);

        _brickContainer.AddChild(brick);
      }
    }

    GetParent().SetDeferred("BrickCount", _gridSize.X * _gridSize.Y - _gridSize.X);
  }

  public void OnBrickDestroyed()
  {
    GetParent().Call("IncreaseScore");
    GetParent().Call("RemoveBrick");
  }
}
