using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

#nullable disable
namespace IUTGame;

public abstract class Game
{
  private IScreen screen;
  private DispatcherTimer timer;
  private List<GameItem> items;
  private List<IKeyboardInteract> keyItems;
  private List<IMouseInteract> mouseItems;
  private List<IGamepadInteract> gpItems;
  private bool animate;
  private List<GameItem> toRemove;
  private List<GameItem> toAdd;
  private Sound backgroundMusic;

  public string Version => "2.4.0";

  public Game(IScreen screen, string spritesFolder, string soundsFolder, int fps = 50)
  {
    if (fps < 10 || fps > 200)
      throw new BadFPS(fps);
    this.screen = screen;
    this.gpItems = new List<IGamepadInteract>();
    this.toRemove = new List<GameItem>();
    this.toAdd = new List<GameItem>();
    this.items = new List<GameItem>();
    screen.InitSpritesStore(spritesFolder);
    SoundStore.LoadSounds(soundsFolder);
    this.timer = new DispatcherTimer(DispatcherPriority.Render);
    this.timer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / fps);
    this.timer.Tick += new EventHandler(this.Timer_Tick);
    this.keyItems = new List<IKeyboardInteract>();
    screen.KeyDown = new KeyEvent(this.Screen_KeyDown);
    screen.KeyUp = new KeyEvent(this.Screen_KeyUp);
    this.mouseItems = new List<IMouseInteract>();
    screen.MouseWheel = new MouseWheelEvent(this.MouseWheel);
    screen.MouseDown = new MouseButtonEvent(this.MouseDown);
    screen.MouseUp = new MouseButtonEvent(this.MouseUp);
    screen.MouseMove = new MouseMoveEvent(this.MouseMove);
  }

  public IScreen Screen => this.screen;

  internal static void RunOnUIThread(Action a) => Application.Current?.Dispatcher.Invoke(a);

  private void MouseWheel(int delta)
  {
    foreach (IMouseInteract mouseItem in this.mouseItems)
      mouseItem.MouseWheel(delta);
  }

  private void MouseDown(MouseButton button, double x, double y)
  {
    foreach (IMouseInteract mouseItem in this.mouseItems)
    {
      if (button == MouseButton.Left)
        mouseItem.MouseLeftButtonDown(x, y);
      else
        mouseItem.MouseRightButtonDown(x, y);
    }
  }

  private void MouseUp(MouseButton button, double x, double y)
  {
    foreach (IMouseInteract mouseItem in this.mouseItems)
    {
      if (button == MouseButton.Left)
        mouseItem.MouseLeftButtonUp(x, y);
      else
        mouseItem.MouseRightButtonUp(x, y);
    }
  }

  private void MouseMove(double x, double y)
  {
    foreach (IMouseInteract mouseItem in this.mouseItems)
      mouseItem.MouseMoved(x, y);
  }

  private void Screen_KeyUp(Key key)
  {
    foreach (IKeyboardInteract keyItem in this.keyItems)
      keyItem.KeyUp(key);
  }

  private void Screen_KeyDown(Key key)
  {
    foreach (IKeyboardInteract keyItem in this.keyItems)
      keyItem.KeyDown(key);
  }

  protected GameItem[] ListItems() => this.items.ToArray();

  public void AddItem(GameItem item) => this.toAdd.Add(item);

  public void RemoveItem(GameItem item) => this.toRemove.Add(item);

  public void Run()
  {
    this.InitItems();
    this.Resume();
  }

  public void Resume()
  {
    this.screen.Focus();
    this.timer.Start();
  }

  private void Timer_Tick(object sender, EventArgs e)
  {
    if (this.animate)
      return;
    this.animate = true;
    foreach (GameItem gameItem in this.items)
    {
      if (gameItem is IAnimable animable)
        animable.Animate(this.timer.Interval);
    }
    List<GameItem> all = this.items.FindAll((Predicate<GameItem>) (item => item.Collidable));
    foreach (GameItem gameItem in all)
    {
      GameItem g = gameItem;
      all.ForEach((Action<GameItem>) (item =>
      {
        if (g == item || !g.IsCollide(item))
          return;
        g.CollideEffect(item);
      }));
    }
    this.mouseItems.RemoveAll((Predicate<IMouseInteract>) (item => this.toRemove.Contains((GameItem) item)));
    this.keyItems.RemoveAll((Predicate<IKeyboardInteract>) (item => this.toRemove.Contains((GameItem) item)));
    this.gpItems.RemoveAll((Predicate<IGamepadInteract>) (item => this.toRemove.Contains((GameItem) item)));
    this.items.RemoveAll((Predicate<GameItem>) (item => this.toRemove.Contains(item)));
    this.toRemove.ForEach((Action<GameItem>) (item => item.Dispose()));
    this.toRemove.Clear();
    foreach (GameItem gameItem in this.toAdd)
    {
      this.items.Add(gameItem);
      if (gameItem is IMouseInteract mouseInteract)
        this.mouseItems.Add(mouseInteract);
      if (gameItem is IKeyboardInteract keyboardInteract)
        this.keyItems.Add(keyboardInteract);
      if (gameItem is IGamepadInteract gamepadInteract)
        this.gpItems.Add(gamepadInteract);
    }
    this.toAdd.Clear();
    this.animate = false;
  }

  public void Pause() => this.timer.Stop();

  public bool IsRunning => this.timer.IsEnabled;

  public void Win()
  {
    this.Stop();
    this.RunWhenWin();
  }

  public void Loose()
  {
    this.Stop();
    this.toRemove.Clear();
    this.items.ForEach((Action<GameItem>) (item => this.toRemove.Add(item)));
    this.RunWhenLoose();
  }

  public void PlayBackgroundMusic(string file)
  {
    if (!SoundStore.AllowSounds)
      return;
    this.backgroundMusic = SoundStore.Get(file);
    this.backgroundMusic.Play(true);
  }

  public void StopBackgroundMusic()
  {
    if (!SoundStore.AllowSounds)
      return;
    this.backgroundMusic.Stop();
  }

  public double BackgroundVolume
  {
    get => this.backgroundMusic == null ? 0.0 : this.backgroundMusic.Volume;
    set
    {
      if (this.backgroundMusic == null)
        return;
      this.backgroundMusic.Volume = value;
    }
  }

  private void Stop()
  {
    if (this.backgroundMusic != null)
      this.StopBackgroundMusic();
    this.timer.Stop();
  }

  protected abstract void InitItems();

  protected abstract void RunWhenWin();

  protected abstract void RunWhenLoose();
}
