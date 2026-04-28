namespace IUTGame;

public class SoundNotFoundError : SoundError
{
  private string name;

  public SoundNotFoundError(string name) => this.name = name;

  public string Name => this.name;
}
