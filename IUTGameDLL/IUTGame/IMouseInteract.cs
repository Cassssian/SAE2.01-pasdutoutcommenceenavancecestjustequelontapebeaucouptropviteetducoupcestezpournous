namespace IUTGame;

public interface IMouseInteract
{
  void MouseMoved(double x, double y);

  void MouseLeftButtonDown(double x, double y);

  void MouseLeftButtonUp(double x, double y);

  void MouseRightButtonDown(double x, double y);

  void MouseRightButtonUp(double x, double y);

  void MouseWheel(int delta);
}
