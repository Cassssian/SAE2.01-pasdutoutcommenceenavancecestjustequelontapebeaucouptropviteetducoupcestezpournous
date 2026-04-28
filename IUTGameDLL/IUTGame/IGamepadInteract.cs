namespace IUTGame;

public interface IGamepadInteract
{
  void AxisXChanged(int newValue);

  void AxisYChanged(int newValue);

  void ButtonPressed(int number);
}
