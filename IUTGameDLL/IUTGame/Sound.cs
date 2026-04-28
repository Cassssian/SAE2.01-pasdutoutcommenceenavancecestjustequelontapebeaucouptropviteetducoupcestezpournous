using System;
using System.Windows.Media;

namespace IUTGame;

public class Sound
{
  private MediaPlayer player;
  private bool loop;
  private bool playing;

  public Sound(string path)
  {
    if (!SoundStore.AllowSounds)
      return;
    this.loop = false;
    this.playing = false;
    this.player = new MediaPlayer();
    this.player.MediaEnded += new EventHandler(this.Player_MediaEnded);
    this.player.MediaOpened += new EventHandler(this.Player_MediaOpened);
    this.player.MediaFailed += new EventHandler<ExceptionEventArgs>(this.Player_MediaFailed);
    this.player.Open(new Uri(path));
  }

  private void Player_MediaFailed(object sender, ExceptionEventArgs e)
  {
    this.playing = false;
    throw new SoundError();
  }

  private void Player_MediaOpened(object sender, EventArgs e)
  {
  }

  private void Player_MediaEnded(object sender, EventArgs e)
  {
    this.playing = false;
    if (!this.loop)
      return;
    this.player.Position = TimeSpan.Zero;
    this.player.Play();
  }

  public void Stop()
  {
    this.player.Stop();
    this.playing = false;
  }

  public void Play(bool loop = false)
  {
    if (this.playing || !SoundStore.AllowSounds)
      return;
    this.playing = true;
    this.loop = loop;
    this.player.Position = new TimeSpan(0L);
    this.player.Play();
  }

  public double Volume
  {
    get => this.player.Volume;
    set
    {
      if (!SoundStore.AllowSounds)
        return;
      this.player.Volume = value;
    }
  }
}
