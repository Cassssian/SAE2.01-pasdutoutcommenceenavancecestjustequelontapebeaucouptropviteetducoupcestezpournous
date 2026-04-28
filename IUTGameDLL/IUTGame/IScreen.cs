namespace IUTGame;

public interface IScreen
{
  KeyEvent KeyDown { get; set; }

  KeyEvent KeyUp { get; set; }

  MouseWheelEvent MouseWheel { get; set; }

  MouseButtonEvent MouseDown { get; set; }

  MouseButtonEvent MouseUp { get; set; }

  MouseMoveEvent MouseMove { get; set; }

  void Focus();

  void InitSpritesStore(string spritesFolderName);

  int LoadSprite(string spriteName);

  void RemoveSprite(int spriteID);

  void DrawSprite(
    int spriteID,
    double x,
    double y,
    double angle = 0.0,
    double scaleX = 1.0,
    double scaleY = 1.0,
    int zindex = 0);

  double Width { get; }

  double Height { get; }

  double GetSpriteWidth(int spriteID);

  double GetSpriteHeight(int spriteID);
}
