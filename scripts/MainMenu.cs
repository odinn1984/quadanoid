using System;
using System.Threading.Tasks;
using Godot;

public partial class MainMenu : Node2D
{
  public const string TransitionEffectTimerPath = "TransitionEffectTimer";
  public const string EffectAnimationPlayerPath = "EffectsAnimation";
  public const string StartLevelButtonPath = "Control/StartGame";
  public const string QuitGameButtonPath = "Control/QuitGame";
  public const string BlurEffectPath = "EffectsLayer/BlurEffect";

  private bool _transitioning = false;

  private Button _startLevel;
  private Button _quitGame;
  private Timer _transitionEffectTimer;
  private ColorRect _blurEffect;
  private AnimationPlayer _effectAnimationPlayer;

  public override void _Ready()
	{
    _startLevel = LoadNode<Button>(StartLevelButtonPath);
    _quitGame = LoadNode<Button>(QuitGameButtonPath);
    _transitionEffectTimer = LoadNode<Timer>(TransitionEffectTimerPath);
    _effectAnimationPlayer = LoadNode<AnimationPlayer>(EffectAnimationPlayerPath);
    _blurEffect = LoadNode<ColorRect>(BlurEffectPath);

    _startLevel.Pressed += OnStartLevelPressed;
    _quitGame.Pressed += OnQuitGamePressed;
    _transitionEffectTimer.Timeout += OnTransitionEffectTimeOut;

    _transitionEffectTimer.Start();
    _effectAnimationPlayer.Play("ReverseTransition");
    _blurEffect.Visible = true;
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

  private void OnStartLevelPressed()
  {
    _transitioning = true;
    _transitionEffectTimer.Start();
    _effectAnimationPlayer.Play("Transition");
    _blurEffect.Visible = true;
  }

  private void OnQuitGamePressed()
  {
    GetTree().Quit();
  }

  async private void OnTransitionEffectTimeOut()
  {
    if (_transitioning)
    {
      PackedScene next = ResourceLoader.Load<PackedScene>("res://levels/infinite.tscn");
      await Task.Delay(TimeSpan.FromMilliseconds(1000));
      GetTree().ChangeSceneToPacked(next);
    }
    else
    {
      _blurEffect.Visible = false;
    }
  }

}
