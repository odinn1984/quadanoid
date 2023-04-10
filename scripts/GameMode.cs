using System;
using System.Diagnostics;
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
  public const string PauseMenuPath = "MenuLayer/PauseMenu";
  public const string PauseMenuTitlePath = $"{PauseMenuPath}/MenuText";
  public const string RestartLevelButtonPath = $"{PauseMenuPath}/RestartGame";
  public const string MainMenuButtonPath = $"{PauseMenuPath}/BackToMenu";
  public const string QuitGameButtonPath = $"{PauseMenuPath}/QuitGame";
  public const string MenuEffectPath = "Camera2D/EffectLayer/BlurEffect";
  public const string RespawnEffectPath = "Camera2D/EffectLayer/GlitchEffect";
  public const string RespawnEffectTimerPath = "Timers/RespawnEffectTimer";
  public const string TransitionEffectTimerPath = "Timers/TransitionEffectTimer";
  public const string EffectAnimationPlayerPath = "Camera2D/EffectLayer/EffectsAnimation";
  public const string RespawnTimerPath = "Timers/RespawnTimer";
  public const string RespawnSFXPath = "RespawnSFX";
  public const string MusicPath = "BGMusic";

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
  private bool _restartingScene = false;
  private bool _transitionMainMenu = false;
  private bool _respawning = true;
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
  private ColorRect _menuEffect;
  private ColorRect _respawnEffect;
  private Timer _respawnEffectTimer;
  private Timer _respawnTimer;
  private Timer _transitionEffectTimer;
  private AnimationPlayer _effectAnimationPlayer;
  private CharacterBody2D _ball;
  private CharacterBody2D[] _paddles = {};
  private AudioStreamPlayer2D _respawnSFX;
  private AudioStreamPlayer2D _music;

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
    _menuEffect = LoadNode<ColorRect>(MenuEffectPath);
    _respawnEffect= LoadNode<ColorRect>(RespawnEffectPath);
    _respawnEffectTimer = LoadNode<Timer>(RespawnEffectTimerPath);
    _respawnTimer = LoadNode<Timer>(RespawnTimerPath);
    _transitionEffectTimer = LoadNode<Timer>(TransitionEffectTimerPath);
    _effectAnimationPlayer = LoadNode<AnimationPlayer>(EffectAnimationPlayerPath);
    _respawnSFX = LoadNode<AudioStreamPlayer2D>(RespawnSFXPath);
    _music = LoadNode<AudioStreamPlayer2D>(MusicPath);

    _effectAnimationPlayer.Play("ReverseTransition");
    _transitionEffectTimer.Start();
    _menuEffect.Visible = true;

    _restartLevel.Pressed += OnRestartLevelPressed;
    _mainMenuLevel.Pressed += OnMainMenuPressed;
    _quitGameLevel.Pressed += OnQuitGamePressed;
    _respawnEffectTimer.Timeout += OnRespawnEffectTimeOut;
    _respawnTimer.Timeout += OnRespawnTimeout;
    _transitionEffectTimer.Timeout += OnTransitionEffectTimeOut;

    InitializePaddles();
    LoadHighScore();

    SetMovementState(false, false);
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

    _music.Stop();
    _transitionEffectTimer.Start();
    _effectAnimationPlayer.Play("Transition");
    _restartingScene = true;
    _pauseMenu.Visible = false;
  }

  private void OnMainMenuPressed()
  {
    _music.Stop();
    _transitionEffectTimer.Start();
    _effectAnimationPlayer.Play("Transition");

    _transitionMainMenu = true;
    _restartingScene = false;
    _pauseMenu.Visible = false;

    SaveHighScore();
  }

  private void OnQuitGamePressed()
  {
    _music.Stop();
    SaveHighScore();
    GetTree().Quit();
  }

  private void OnRespawnEffectTimeOut()
  {
    _respawnEffect.Visible = false;
    _respawnTimer.Start();
  }

  private void OnRespawnTimeout()
  {
    _respawning = false;
    SetMovementState(false, true);
  }

  async private void OnTransitionEffectTimeOut()
  {
    if (_restartingScene)
    {
      _music.Stop();
      GetTree().ChangeSceneToFile(GetTree().CurrentScene.SceneFilePath);
    }
    else if (_transitionMainMenu)
    {
      PackedScene next = ResourceLoader.Load<PackedScene>("res://levels/main_menu.tscn");

      _music.Stop();

      await Task.Delay(TimeSpan.FromMilliseconds(1000));

      GetTree().ChangeSceneToPacked(next);
    }
    else
    {
      _menuEffect.Visible = false;
      _respawnEffect.Visible = true;
      _respawnSFX.Play();
      _respawnEffectTimer.Start();
      _respawnTimer.Start();
    }
  }

  public override void _Process(double delta)
	{
    _currentBallPosition = _ball.Position;
    _livesLabel.Text = $"LIVES {_livesRemaining}";
    _scoreLabel.Text = $"SCORE {_score}";
    _highScoreLabel.Text = $"HIGH SCORE {_highScore}";
    _levelLabel.Text = $"LVL {_currentLevel} (+{MathF.Round((_currentSpeed - 1.0f) * 100.0f)}%)";

    if (Input.IsActionJustPressed("Shoot") && _livesRemaining > 0 && BrickCount > 0 && !_gamePaused && !_respawning)
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
      _effectAnimationPlayer.Play("Transition", -1, 2.0f);
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

    if (_ball.Velocity.Length() != 0.0f)
    {
      if (_currentBallPosition.X < _viewportBounds.Position.X - 10.0f || _currentBallPosition.X > _viewportBounds.Size.X + 10.0f)
      {
        Respawn();
      }

      if (_currentBallPosition.Y < _viewportBounds.Position.Y - 10.0f || 
          _currentBallPosition.Y > _viewportBounds.Size.Y - 60.0f)
      {
        Respawn();
      }
    }
  }

  public void Respawn(bool outOfBounds = true)
  {
    _respawning = true;
    _ballShot = false;
    
    SetMovementState(false, false);

    if (outOfBounds)
    {
      DecreaseScore();
      DecreaseLives();

      if (_livesRemaining > 0)
      {
        _respawnSFX.Play();

        _respawnEffect.Visible = true;
        _respawnEffectTimer.Start();
        _respawnTimer.Start();
      }
    }

    if (_livesRemaining == 0)
    {
      return;
    }

    _ball.Call("Respawn");

    foreach (CharacterBody2D paddle in _paddles)
    {
      paddle.Call("Respawn");
    }
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
    _ball.Set("Velocity", Vector2.Zero);

    foreach (CharacterBody2D paddle in _paddles)
    {
      paddle.Set("CanMove", paddles);
      paddle.Set("Velocity", Vector2.Zero);
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
    _music.PitchScale = _pauseMenu.Visible ? 1.0f : 0.5f;

    _pauseMenuLabel.Text = title.ToUpper();
    _pauseMenu.Visible = !_pauseMenu.Visible;
    _menuEffect.Visible = !_menuEffect.Visible;
    _gamePaused = _pauseMenu.Visible;

    SetMovementState(!_gamePaused && _ballShot && _livesRemaining > 0, !_gamePaused && _livesRemaining > 0);
  }
}
