using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IUTGame;
using System.Collections.Generic;
using VraiPseudoSae.Utils.AudioPlayer;
using VraiPseudoSae.Utils.SaveManager;
using VraiPseudoSae.view.Flappy_bird;
using VraiPseudoSae.view.Maze;
using VraiPseudoSae.view.gameintro;
using VraiPseudoSae.view.RLS_Pages;

namespace VraiPseudoSae.view.hub
{
    public class HubGame : Game
    {
        private const string HubMusicAlias = "hub";
        private const string HubMusicPath = "Assets/Firewhole - love 2.mp3";
        private const float HubMusicBaseVolume = 0.2f;

        private readonly HomePage homePage;
        private readonly Canvas footballZone;
        private readonly Canvas mazeZone;
        private readonly Canvas rlsZone;
        private readonly Canvas pinZone;
        private readonly Canvas flappyZone;
        private readonly GameIntroPlayerSpriteSet playerSprites;
        private readonly List<HubLaunchZone> launchZones = new();
        private readonly Rect settingsPanelInteractionBounds = new(624, 384, 32, 32);
        private readonly Rect settingsPanelCollisionBounds = new(624, 384, 32, 16);
        private readonly JsonPakAudioService audio = new();

        private HubPlayer player = null!;
        private ParametresJeuSauvegarde settings;
        private LoopingSoundHandle? hubMusic;
        private bool audioDisposed;

        public HubGame(
            IScreen screen,
            string spritesFolder,
            string soundsFolder,
            GameIntroPlayerSpriteSet playerSprites,
            HomePage homePage,
            Canvas footballZone,
            Canvas mazeZone,
            Canvas rlsZone,
            Canvas pinZone,
            Canvas flappyZone,
            ParametresJeuSauvegarde settings)
            : base(screen, spritesFolder, soundsFolder, 60)
        {
            this.playerSprites = playerSprites;
            this.homePage = homePage;
            this.footballZone = footballZone;
            this.mazeZone = mazeZone;
            this.rlsZone = rlsZone;
            this.pinZone = pinZone;
            this.flappyZone = flappyZone;
            this.settings = settings.Normaliser();
            audio.LoadFromPath(HubMusicPath, HubMusicAlias);
        }

        protected override void InitItems()
        {
            launchZones.Clear();
            launchZones.Add(new HubLaunchZone(footballZone, "mini-jeu FOOT", LaunchFootballGame));
            launchZones.Add(new HubLaunchZone(mazeZone, "mini-jeu LABYRINTHE", LaunchMazeGame));
            launchZones.Add(new HubLaunchZone(rlsZone, "mini-jeu RLS", LaunchRls));
            launchZones.Add(new HubLaunchZone(pinZone, "NUIT DU CODE", LaunchPin));
            launchZones.Add(new HubLaunchZone(flappyZone, "Flappy Bird", LaunchFlappy));

            player = new HubPlayer(608, 374, this, playerSprites);
            player.ApplySettings(settings);
            AddItem(player);

            Panel.SetZIndex(footballZone, (int)Canvas.GetTop(footballZone));
            Panel.SetZIndex(mazeZone, (int)Canvas.GetTop(mazeZone));
            Panel.SetZIndex(rlsZone, (int)Canvas.GetTop(rlsZone));
            Panel.SetZIndex(pinZone, (int)Canvas.GetTop(pinZone));
            Panel.SetZIndex(flappyZone, (int)Canvas.GetTop(flappyZone));
            StartHubMusic();
            
        }

        public Point PlayerCenter => player.Center;

        public void UpdateInfoText()
        {
            homePage.CenterCameraOn(player.Center);

            if (IsInSettingsPanelInteraction(player.InteractionBounds))
            {
                homePage.SetInfoText($"Appuie sur {HomePage.FormatKey(InteractionKey)} pour ouvrir les paramètres");
                homePage.SetPanelKeyHintVisible(true);
                return;
            }

            homePage.SetPanelKeyHintVisible(false);
            HubLaunchZone? zone = GetLaunchZoneAt(player.InteractionBounds);
            homePage.SetInfoText(zone is null
                ? "Va vers une case"
                : $"Appuie sur {HomePage.FormatKey(InteractionKey)} pour lancer {zone.DisplayName}");
        }

        public void TryLaunchMiniGame()
        {
            if (IsInSettingsPanelInteraction(player.InteractionBounds))
            {
                homePage.OpenGameSettings();
                return;
            }

            HubLaunchZone? zone = GetLaunchZoneAt(player.InteractionBounds);
            if (zone is null)
                return;

            zone.Launch();
            StopHubMusic();
        }

        public bool IsBlockedByLaunchZone(Rect bounds)
        {
            if (Overlaps(bounds, settingsPanelCollisionBounds))
                return true;

            foreach (HubLaunchZone zone in launchZones)
            {
                if (Overlaps(bounds, zone.Bounds))
                    return true;
            }

            return false;
        }

        public void ApplySettings(ParametresJeuSauvegarde nextSettings)
        {
            settings = nextSettings.Normaliser();
            player?.ApplySettings(settings);
            if (hubMusic is not null)
                hubMusic.Volume = GetHubMusicVolume();
            UpdateInfoText();
        }

        public void SetInputLocked(bool locked)
        {
            player?.SetInputLocked(locked);
        }

        public void StopHubMusic()
        {
            hubMusic?.Dispose();
            hubMusic = null;

            if (audioDisposed)
                return;

            audio.Dispose();
            audioDisposed = true;
        }

        private void StartHubMusic()
        {
            if (audioDisposed)
                return;

            if (hubMusic is not null)
            {
                hubMusic.Volume = GetHubMusicVolume();
                return;
            }

            hubMusic = audio.PlayLooping(HubMusicAlias, GetHubMusicVolume());
        }

        private float GetHubMusicVolume()
        {
            return (float)(HubMusicBaseVolume
                           * settings.VolumeGeneral / 100.0
                           * settings.VolumeMusique / 100.0);
        }

        private HubLaunchZone? GetLaunchZoneAt(Rect interactionBounds)
        {
            foreach (HubLaunchZone zone in launchZones)
            {
                if (Overlaps(interactionBounds, zone.Bounds))
                    return zone;
            }

            return null;
        }

        private bool IsInSettingsPanelInteraction(Rect interactionBounds)
        {
            return Overlaps(interactionBounds, settingsPanelInteractionBounds);
        }

        private Key InteractionKey => HomePage.ParseSavedKey(settings.ToucheInteraction, Key.E);

        private void LaunchFootballGame()
        {
            FootGame display = new FootGame();
            display.Show();
        }

        private void LaunchMazeGame()
        {
            MazeWindow display = new MazeWindow();
            display.Show();
        }

        private void LaunchRls()
        {
            RLS display = new RLS();
            display.Show();
        }

        private void LaunchPin()
        {
            homePage.OpenWizardSurvival();
        }

        private void LaunchFlappy()
        {
            FlappyBird display = new();
            display.Show();
        }

        private static bool Overlaps(Rect a, Rect b)
        {
            return a.Left < b.Right
                   && a.Right > b.Left
                   && a.Top < b.Bottom
                   && a.Bottom > b.Top;
        }

        protected override void RunWhenWin()
        {
        }

        protected override void RunWhenLoose()
        {
        }

        private sealed class HubLaunchZone(Canvas visual, string displayName, Action launch)
        {
            public string DisplayName { get; } = displayName;

            public Rect Bounds => new(
                Canvas.GetLeft(visual),
                Canvas.GetTop(visual),
                visual.Width,
                visual.Height);

            public void Launch()
            {
                launch();
            }
        }
    }
}
