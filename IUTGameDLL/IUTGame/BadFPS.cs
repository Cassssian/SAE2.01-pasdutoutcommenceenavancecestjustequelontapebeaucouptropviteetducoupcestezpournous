namespace IUTGame;

public class BadFPS : GameException
{
  private int fps;

  public BadFPS(int fps)
    : base()
  {
    this.fps = fps;
  }

  public int FPS => this.fps;
}
