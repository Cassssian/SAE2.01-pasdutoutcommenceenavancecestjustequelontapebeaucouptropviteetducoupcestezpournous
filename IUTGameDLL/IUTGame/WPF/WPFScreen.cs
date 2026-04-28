using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

#nullable disable
namespace IUTGame.WPF;

public class WPFScreen : IScreen
{
  private Canvas canvas;
  private KeyEvent keyDown;
  private KeyEvent keyUp;
  private MouseWheelEvent mouseWheel;
  private MouseButtonEvent mouseDown;
  private MouseButtonEvent mouseUp;
  private MouseMoveEvent mouseMove;
  private List<Sprite> sprites;
  private SpriteStore spriteStore;

  public WPFScreen(Canvas canvas)
  {
    WPFScreen wpfScreen = this;
    this.spriteStore = new SpriteStore();
    this.sprites = new List<Sprite>();
    this.canvas = canvas;
    canvas.Focusable = true;
    Keyboard.Focus((IInputElement) canvas);
    canvas.Cursor = Cursors.None;
    canvas.KeyDown += (KeyEventHandler) ((s, e) =>
    {
      KeyEvent keyDown = wpfScreen.KeyDown;
      if (keyDown == null)
        return;
      keyDown(e.Key);
    });
    canvas.KeyUp += (KeyEventHandler) ((s, e) =>
    {
      KeyEvent keyUp = wpfScreen.KeyUp;
      if (keyUp == null)
        return;
      keyUp(e.Key);
    });
    canvas.PreviewMouseWheel += (MouseWheelEventHandler) ((s, e) =>
    {
      MouseWheelEvent mouseWheel = wpfScreen.MouseWheel;
      if (mouseWheel == null)
        return;
      mouseWheel(e.Delta);
    });
    canvas.PreviewMouseDown += (MouseButtonEventHandler) ((s, e) =>
    {
      System.Windows.Point position = e.GetPosition((IInputElement) canvas);
      double x = position.X;
      double y = position.Y;
      MouseButtonEvent mouseDown = wpfScreen.MouseDown;
      if (mouseDown == null)
        return;
      mouseDown(e.ChangedButton, x, y);
    });
    canvas.PreviewMouseUp += (MouseButtonEventHandler) ((s, e) =>
    {
      System.Windows.Point position = e.GetPosition((IInputElement) canvas);
      double x = position.X;
      double y = position.Y;
      MouseButtonEvent mouseUp = wpfScreen.MouseUp;
      if (mouseUp == null)
        return;
      mouseUp(e.ChangedButton, x, y);
    });
    canvas.PreviewMouseMove += (MouseEventHandler) ((s, e) =>
    {
      System.Windows.Point position = e.GetPosition((IInputElement) canvas);
      double x = position.X;
      double y = position.Y;
      MouseMoveEvent mouseMove = wpfScreen.MouseMove;
      if (mouseMove == null)
        return;
      mouseMove(x, y);
    });
  }

  public KeyEvent KeyDown
  {
    get => this.keyDown;
    set => this.keyDown = value;
  }

  public KeyEvent KeyUp
  {
    get => this.keyUp;
    set => this.keyUp = value;
  }

  public MouseWheelEvent MouseWheel
  {
    get => this.mouseWheel;
    set => this.mouseWheel = value;
  }

  public MouseButtonEvent MouseDown
  {
    get => this.mouseDown;
    set => this.mouseDown = value;
  }

  public MouseButtonEvent MouseUp
  {
    get => this.mouseUp;
    set => this.mouseUp = value;
  }

  public MouseMoveEvent MouseMove
  {
    get => this.mouseMove;
    set => this.mouseMove = value;
  }

  public double Width => this.canvas.ActualWidth;

  public double Height => this.canvas.ActualHeight;

  public void Focus() => this.canvas.Focus();

  public void InitSpritesStore(string spritesFolderName)
  {
    this.spriteStore.ResFolderName = spritesFolderName;
  }

  public int LoadSprite(string spriteName)
  {
    Sprite sprite = this.spriteStore.Get(spriteName);
    this.sprites.Add(sprite);
    this.canvas.Children.Add((UIElement) sprite.Image);
    return this.sprites.Count - 1;
  }

  public void RemoveSprite(int spriteID)
  {
    if (spriteID <= -1)
      return;
    this.canvas.Children.Remove((UIElement) this.sprites[spriteID].Image);
  }

  public void DrawSprite(
    int spriteID,
    double x,
    double y,
    double angle = 0.0,
    double scaleX = 1.0,
    double scaleY = 1.0,
    int zindex = 1)
  {
    this.sprites[spriteID].Draw(x, y, angle, scaleX, scaleY, zindex);
  }

  public double GetSpriteWidth(int spriteID)
  {
    double spriteWidth = 0.0;
    if (spriteID > -1 && spriteID < this.sprites.Count)
    {
      Sprite sprite = this.sprites[spriteID];
      if (sprite != null)
        spriteWidth = sprite.Image.ActualWidth;
    }
    return spriteWidth;
  }

  public double GetSpriteHeight(int spriteID)
  {
    double spriteHeight = 0.0;
    if (spriteID > -1 && spriteID < this.sprites.Count)
    {
      Sprite sprite = this.sprites[spriteID];
      if (sprite != null)
        spriteHeight = sprite.Image.ActualHeight;
    }
    return spriteHeight;
  }
}
