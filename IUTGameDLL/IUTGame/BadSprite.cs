namespace IUTGame;

public class BadSprite : GameException
{
  public string Name { get; }

  public BadSprite(string name)
    : base()
  {
    this.Name = name;
  }
}
