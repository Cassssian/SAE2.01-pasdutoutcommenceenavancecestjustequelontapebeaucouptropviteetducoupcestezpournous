// Decompiled with JetBrains decompiler
// Type: IUTGame.WPF.Sprite
// Assembly: IUTGame, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87724564-3D2F-46BB-8B18-E7BE9CE1E606
// Assembly location: C:\Users\Asus\Downloads\Compressed\Aide SAE2.01 2025\Aide SAE2.01 2025\IUTGame.dll

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#nullable disable
namespace IUTGame.WPF;

public class Sprite
{
  private Image image;

  public Sprite(Image image) => this.image = image;

  public Image Image => this.image;

  public void Draw(double x, double y, double angle = 0.0, double scaleX = 1.0, double scaleY = 1.0, int zindex = 0)
  {
    TransformGroup transformGroup = new TransformGroup();
    RotateTransform rotateTransform = new RotateTransform(angle);
    transformGroup.Children.Add((Transform) rotateTransform);
    transformGroup.Children.Add((Transform) new ScaleTransform(scaleX, scaleY)
    {
      CenterX = 0.5,
      CenterY = 0.5
    });
    this.image.RenderTransform = (Transform) transformGroup;
    this.image.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
    Canvas.SetLeft((UIElement) this.image, x);
    Canvas.SetTop((UIElement) this.image, y);
    Panel.SetZIndex((UIElement) this.image, zindex);
    this.image.InvalidateArrange();
    this.image.UpdateLayout();
  }
}
