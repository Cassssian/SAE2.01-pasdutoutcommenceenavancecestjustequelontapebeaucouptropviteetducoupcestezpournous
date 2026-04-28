using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace IUTGame;

public static class SoundStore
{
  private static Dictionary<string, Sound> sounds;
  private static bool allowSounds = true;

  public static bool AllowSounds
  {
    get => SoundStore.allowSounds;
    set => SoundStore.allowSounds = value;
  }

  static SoundStore() => SoundStore.sounds = new Dictionary<string, Sound>();

  public static void LoadSounds(string folder)
  {
    if (!SoundStore.allowSounds)
      return;
    string str = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), folder);
    if (!Directory.Exists(str))
      return;
    foreach (string file in Directory.GetFiles(str))
    {
      string fileName = Path.GetFileName(file);
      SoundStore.sounds[fileName] = new Sound(file);
    }
  }

  public static Sound Get(string name)
  {
    return SoundStore.sounds.ContainsKey(name) ? SoundStore.sounds[name] : throw new SoundNotFoundError(name);
  }
}
