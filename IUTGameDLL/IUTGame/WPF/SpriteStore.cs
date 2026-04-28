// Decompiled with JetBrains decompiler
// Type: IUTGame.WPF.SpriteStore
// Assembly: IUTGame, Version=2.4.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 87724564-3D2F-46BB-8B18-E7BE9CE1E606
// Assembly location: C:\Users\Asus\Downloads\Compressed\Aide SAE2.01 2025\Aide SAE2.01 2025\IUTGame.dll

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#nullable disable
namespace IUTGame.WPF;

public class SpriteStore
{
  private string resFolderName;
  private Dictionary<string, BitmapImage> bitmaps;

  public SpriteStore()
  {
    this.resFolderName = "";
    this.bitmaps = new Dictionary<string, BitmapImage>();
  }

  public string ResFolderName
  {
    get => this.resFolderName;
    set => this.resFolderName = value;
  }

  public Sprite Get(string name)
  {
    BitmapImage bitmapImage;
    if (!this.bitmaps.ContainsKey(name))
    {
      try
      {
        bitmapImage = new BitmapImage(new Uri($"pack://application:,,,/{this.resFolderName}/{name}"));
        this.bitmaps[name] = bitmapImage;
      }
      catch
      {
        throw new BadSprite(name);
      }
    }
    else
      bitmapImage = this.bitmaps[name];
    return new Sprite(new Image()
    {
      Source = (ImageSource) bitmapImage
    });
  }
}
