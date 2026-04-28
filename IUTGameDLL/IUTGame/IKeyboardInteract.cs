using System.Windows.Input;

namespace IUTGame;

public interface IKeyboardInteract
{
  void KeyUp(Key key);

  void KeyDown(Key key);
}
