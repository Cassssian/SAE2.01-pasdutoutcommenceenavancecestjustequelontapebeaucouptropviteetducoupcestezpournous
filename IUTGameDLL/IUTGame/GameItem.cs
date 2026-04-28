using System;

namespace IUTGame;

public abstract class GameItem : IDisposable
{
  private double x;
  private double y;
  private Game game;
  private double angle;
  private int spriteID;
  private double scaleX;
  private double scaleY;
  private bool collide = true;
  private int zindex;

  public GameItem(double x, double y, Game game, string spriteName = "", int zindex = 0)
  {
    this.game = game;
    this.x = x;
    this.y = y;
    this.zindex = zindex;
    this.angle = 0.0;
    this.scaleX = 1.0;
    this.scaleY = 1.0;
    this.spriteID = -1;
    if (string.IsNullOrEmpty(spriteName))
      return;
    this.spriteID = this.LoadSprite(spriteName);
    this.Put();
  }

  protected Game TheGame => this.game;

  public double GameWidth => this.TheGame.Screen.Width;

  public double GameHeight => this.TheGame.Screen.Height;

  public void Dispose() => this.TheGame.Screen.RemoveSprite(this.spriteID);

  public abstract string TypeName { get; }

  private int LoadSprite(string spriteName) => this.TheGame.Screen.LoadSprite(spriteName);

  protected void ChangeScale(double sx, double sy)
  {
    this.scaleX = sx;
    this.scaleY = sy;
    this.Put();
  }

  protected void ChangeSprite(string name)
  {
    if (string.IsNullOrEmpty(name))
      throw new BadSprite(name);
    Game.RunOnUIThread((Action) (() =>
    {
      this.TheGame.Screen.RemoveSprite(this.spriteID);
      this.spriteID = this.LoadSprite(name);
    }));
    this.scaleX = this.scaleY = 1.0;
    this.Put();
  }

  public double Orientation
  {
    get => this.angle;
    protected set
    {
      this.angle = value;
      this.Put();
    }
  }

  public int ZIndex
  {
    get => this.zindex;
    set
    {
      this.zindex = value;
      this.Put();
    }
  }

  protected void PutXY(double x, double y)
  {
    this.Left = x;
    this.Top = y;
    this.Put();
  }

  private void Put()
  {
    Game.RunOnUIThread((Action) (() => this.TheGame.Screen.DrawSprite(this.spriteID, this.x, this.y, this.angle, this.scaleX, this.scaleY, this.zindex)));
  }

  protected void MoveXY(double dx, double dy)
  {
    this.x += dx;
    this.y += dy;
    this.Put();
  }

  protected void MoveDA(double distance, double direction)
  {
    this.MoveXY(distance * Math.Cos(direction * Math.PI / 180.0), distance * Math.Sin(direction * Math.PI / 180.0));
  }

  public double Left
  {
    get => this.x - this.Width * (1.0 - 1.0 / this.scaleX) / 2.0;
    protected set
    {
      this.x = value + this.Width * (1.0 - 1.0 / this.scaleX) / 2.0;
      this.Put();
    }
  }

  public double Top
  {
    get => this.y - this.Height * (1.0 - 1.0 / this.scaleY) / 2.0;
    protected set
    {
      this.y = value + this.Height * (1.0 - 1.0 / this.scaleY) / 2.0;
      this.Put();
    }
  }

  public double Width
  {
    get
    {
      double num = 0.0;
      if (this.spriteID >= 0)
        num = this.TheGame.Screen.GetSpriteWidth(this.spriteID);
      return Math.Abs(num * this.scaleX);
    }
  }

  public double Height
  {
    get
    {
      double num = 0.0;
      if (this.spriteID >= 0)
        num = this.TheGame.Screen.GetSpriteHeight(this.spriteID);
      return Math.Abs(num * this.scaleY);
    }
  }

  public double MiddleX => (this.Left + this.Right) / 2.0;

  public double MiddleY => (this.Top + this.Bottom) / 2.0;

  public double Right
  {
    get => this.Left + this.Width;
    protected set
    {
      this.Left = value - this.Width;
      this.Put();
    }
  }

  public double Bottom
  {
    get => this.Top + this.Height;
    protected set
    {
      this.Top = value - this.Height;
      this.Put();
    }
  }

  public bool Collidable
  {
    get => this.collide;
    set => this.collide = value;
  }

  public virtual bool IsCollide(GameItem other)
  {
    return this.Left <= other.Right && other.Left <= this.Right && this.Top <= other.Bottom && other.Top <= this.Bottom;
  }

  public abstract void CollideEffect(GameItem other);

  protected void PlaySound(string file)
  {
    if (!SoundStore.AllowSounds)
      return;
    SoundStore.Get(file).Play();
  }
}
