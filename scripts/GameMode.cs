using System;
using Godot;
using System.Linq;
using System.Threading.Tasks;

public partial class GameMode : Node2D
{
  public const string BallPath = "Ball";
  public const string PaddlesContainerPath = "Paddles";
  public const string ScoreLabelPath = "HUD/TopHUDBorder/TopHUD/TopHUDLeftContainer/Score";
  public const string HighScoreLabelPath = "HUD/TopHUDBorder/TopHUD/TopHUDRightContainer/HighScore";
  public const string LivesLabelPath = "HUD/BottomHUDBorder/BottomHUD/BottomHUDLeftContainer/Lives";
  public const string LevelLabelPath = "HUD/BottomHUDBorder/BottomHUD/BottomHUDRightContainer/Level";
  public const string BrickMatrixPath = "BrickMatrix";
  public const string PauseMenuPath = "PauseMenu";
  public const string PauseMenuTitlePath = "PauseMenu/MenuText";
  public const string RestartLevelButtonPath = "PauseMenu/RestartGame";
  public const string MainMenuButtonPath = "PauseMenu/BackToMenu";
  public const string QuitGameButtonPath = "PauseMenu/QuitGame";

  public const string SaveFilePath = "user://save.data";

  [Export(PropertyHint.Range, "0.01,1.0,0.01")]
  private float _speedIncreaseOnClearPercent = 0.1f;

  [Export(PropertyHint.Range, "1,100,1")]
  private uint _maxLives = 10;

  [Export(PropertyHint.Range, "1,50,1")]
  private uint _scoreOnBrickBreak = 25;

  [Export(PropertyHint.Range, "1,50,1")]
  private uint _scoreDecreaseOnBallOutOfBounds = 50;

  private uint BrickCount { get; set; } = 0;

  private bool _levelRestarting = false;
  private bool _gamePaused = false;
  private bool _ballShot = false;
  private uint _score = 0;
  private uint _highScore = 0;
  private uint _livesRemaining;
  private uint _currentLevel = 1;
  private float _currentSpeed = 1.0f;

  private Vector2 _currentBallPosition;
  private Rect2 _viewportBounds;

  private Label _scoreLabel;
  private Label _highScoreLabel;
  private Label _livesLabel;
  private Label _levelLabel;
  private Label _pauseMenuLabel;
  private Button _restartLevel;
  private Button _mainMenuLevel;
  private Button _quitGameLevel;
  private Control _brickMatrix;
  private Control _pauseMenu;
  private CharacterBody2D _ball;
  private CharacterBody2D[] _paddles = {};

  public override void _Ready()
  {
    _livesRemaining = _maxLives;
    _viewportBounds = GetViewport().GetVisibleRect();

    _ball = LoadNode<CharacterBody2D>(BallPath);
    _scoreLabel = LoadNode<Label>(ScoreLabelPath);
    _highScoreLabel = LoadNode<Label>(HighScoreLabelPath);
    _livesLabel = LoadNode<Label>(LivesLabelPath);
    _levelLabel = LoadNode<Label>(LevelLabelPath);
    _brickMatrix = LoadNode<Control>(BrickMatrixPath);
    _pauseMenu = LoadNode<Control>(PauseMenuPath);
    _pauseMenuLabel = LoadNode<Label>(PauseMenuTitlePath);
    _restartLevel = LoadNode<Button>(RestartLevelButtonPath);
    _mainMenuLevel = LoadNode<Button>(MainMenuButtonPath);
    _quitGameLevel = LoadNode<Button>(QuitGameButtonPath);

    _restartLevel.Pressed += OnRestartLevelPressed;
    _mainMenuLevel.Pressed += OnMainMenuPressed;
    _quitGameLevel.Pressed += OnQuitGamePressed;

    InitializePaddles();
    LoadHighScore();
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

  private void InitializePaddles()
  {
    foreach (Node2D child in LoadNode<Node2D>(PaddlesContainerPath).GetChildren(true))
    {
      if (child.IsInGroup("Paddles"))
      {
        _paddles = _paddles.Append((CharacterBody2D)child).ToArray();
      }
    }

    if (_paddles.Length == 0)
    {
      GD.PrintErr($"Couldn't find object under the path: {PaddlesContainerPath}");
    }
  }

  private void OnRestartLevelPressed()
  {
    SaveHighScore();
    GetTree().ChangeSceneToFile(GetTree().CurrentScene.SceneFilePath);
  }

  private void OnMainMenuPressed()
  {
    SaveHighScore();
  }

  private void OnQuitGamePressed()
  {
    SaveHighScore();
    GetTree().Quit();
  }

  public override void _Process(double delta)
	{
    _currentBallPosition = _ball.Position;
    _livesLabel.Text = $"LIVES {_livesRemaining}";
    _scoreLabel.Text = $"SCORE {_score}";
    _highScoreLabel.Text = $"HIGH SCORE {_highScore}";
    _levelLabel.Text = $"LVL {_currentLevel} (+{MathF.Round((_currentSpeed - 1.0f) * 100.0f)}%)";

    if (Input.IsActionJustPressed("Shoot") && _livesRemaining > 0 && BrickCount > 0 && !_gamePaused)
    {
      _ballShot = true;
      SetMovementState(true, true);
    }

    if (Input.IsActionJustPressed("Pause"))
    {
      ShowPauseMenu();
    }

    if (BrickCount == 0 && !_levelRestarting)
    {
      RestartLevel();
    }

    if (_livesRemaining == 0 && !_gamePaused)
    {
      EndGame();
    }
  }

  public override void _PhysicsProcess(double delta)
  {
    if (_gamePaused)
    {
      return;
    }

    if (_currentBallPosition.X < _viewportBounds.Position.X || _currentBallPosition.X > _viewportBounds.Size.X)
    {
      Respawn();
    }

    if (_currentBallPosition.Y < _viewportBounds.Position.Y || _currentBallPosition.Y > _viewportBounds.Size.Y)
    {
      Respawn();
    }
  }

  public void Respawn(bool outOfBounds = true)
  {
    if (outOfBounds)
    {
      DecreaseScore();
      DecreaseLives();
    }

    SetMovementState(false, false);

    if (_livesRemaining == 0)
    {
      return;
    }

    _ball.Call("Respawn");

    foreach (CharacterBody2D paddle in _paddles)
    {
      paddle.Call("Respawn");
    }

    _ballShot = false;
  }

  public void RemoveBrick()
  {
    if (BrickCount > 0)
    {
      BrickCount--;
    }
  }

  public void IncreaseScore()
  {
    float bonus = (_currentLevel - 1) * _speedIncreaseOnClearPercent;

    _score += (uint)Math.Round(_scoreOnBrickBreak * (bonus > 0.0f ? 1 + bonus : 1.0f));
  }

  public void DecreaseScore()
  {
    if (_score > 0)
    {
      if (_scoreDecreaseOnBallOutOfBounds > _score)
      {
        _score = 0;
      }
      else
      {
        _score -= _scoreDecreaseOnBallOutOfBounds;
      }
    }
  }

  public void DecreaseLives()
  {
    if (_livesRemaining > 0)
    {
      _livesRemaining--;
    }
  }

  async public void RestartLevel()
  {
    _levelRestarting = true;

    SetMovementState(false, false);

    await Task.Delay(TimeSpan.FromMilliseconds(1500));

    _currentLevel++;
    _brickMatrix.Call("CreateBricks");

    Respawn(false);
    IncreaseSpeed();

    _levelRestarting = false;
  }

  private void EndGame()
  {
    SetMovementState(false, false);

    if (_score > _highScore)
    {
      _highScore = _score;
      
      SaveHighScore();
    }

    ShowPauseMenu("GAME OVER");
  }

  private void SetMovementState(bool ball, bool paddles)
  {
    _ball.Set("CanMove", ball);

    foreach (CharacterBody2D paddle in _paddles)
    {
      paddle.Set("CanMove", paddles);
    }
  }

  private void IncreaseSpeed()
  {
    _currentSpeed += _speedIncreaseOnClearPercent;

    _ball.Call("SetSpeedPercent", _currentSpeed);

    foreach (CharacterBody2D paddle in _paddles)
    {
      paddle.Call("SetSpeedPercent", _currentSpeed);
    }
  }

  private void SaveHighScore()
  {
    using var saveGame = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Write);

    saveGame.StoreLine(_highScore.ToString());
  }

  private void LoadHighScore()
  {
    if (!FileAccess.FileExists(SaveFilePath))
    {
      return;
    }

    using var saveGame = FileAccess.Open(SaveFilePath, FileAccess.ModeFlags.Read);
    string savedHighScore = saveGame.GetLine();
    
    _highScore = Convert.ToUInt32(savedHighScore);
  }

  private void ShowPauseMenu(string title = "MENU")
  {
    _pauseMenuLabel.Text = title.ToUpper();

      _pauseMenu.Visible = !_pauseMenu.Visible;
    _gamePaused = _pauseMenu.Visible;

    SetMovementState(!_gamePaused && _ballShot, !_gamePaused);
  }
}