using System;
using System.Windows;
using System.Windows.Controls;
using IUTGame;
using System.Collections.Generic;
using System.Windows.Media;
using VraiPseudoSae.view.RLS_Pages;

namespace VraiPseudoSae.view.hub
{
    public class HubGame : Game
    {
        private readonly HomePage homePage;
        private readonly Canvas footballZone;
        private readonly Canvas mazeZone;
        private readonly Canvas rlsZone;

        private HubPlayer player = null!;

        public HubGame(
            IScreen screen,
            string spritesFolder,
            string soundsFolder,
            HomePage homePage,
            Canvas footballZone,
            Canvas mazeZone,
            Canvas rlsZone)
            : base(screen, spritesFolder, soundsFolder, 60)
        {
            this.homePage = homePage;
            this.footballZone = footballZone;
            this.mazeZone = mazeZone;
            this.rlsZone = rlsZone;
        }

        protected override void InitItems()
        {
            player = new HubPlayer(610, 520, this);
            AddItem(player);

            Panel.SetZIndex(footballZone, (int)Canvas.GetTop(footballZone));
            Panel.SetZIndex(mazeZone, (int)Canvas.GetTop(mazeZone));
            Panel.SetZIndex(rlsZone, (int)Canvas.GetTop(rlsZone));
        }

        public Point PlayerCenter => player.Center;

        public void UpdateInfoText()
        {
            Point playerCenter = player.Center;

            Point footballCenter = new Point(
                Canvas.GetLeft(footballZone) + 85,
                Canvas.GetTop(footballZone) + 30);

            Point mazeCenter = new Point(
                Canvas.GetLeft(mazeZone) + 85,
                Canvas.GetTop(mazeZone) + 30);

            Point rlsCenter = new Point(
                Canvas.GetLeft(rlsZone) + 85,
                Canvas.GetTop(rlsZone) + 30);

            double footballDist = Distance(playerCenter, footballCenter);
            double mazeDist = Distance(playerCenter, mazeCenter);
            double rlsDist = Distance(playerCenter, rlsCenter);

            if (footballDist < 90)
                homePage.SetInfoText("Appuie sur E pour lancer le mini-jeu FOOT");
            else if (mazeDist < 90)
                homePage.SetInfoText("Appuie sur E pour lancer le mini-jeu LABYRINTHE");
            else if (rlsDist < 90)
                homePage.SetInfoText("Appuie sur E pour lancer le mini-jeu RLS");
            else
                homePage.SetInfoText("Va vers une zone");
        }

        public void TryLaunchMiniGame()
        {
            Point playerCenter = player.Center;

            Point footballCenter = new Point(
                Canvas.GetLeft(footballZone) + 85,
                Canvas.GetTop(footballZone) + 30);

            Point mazeCenter = new Point(
                Canvas.GetLeft(mazeZone) + 85,
                Canvas.GetTop(mazeZone) + 30);

            Point rlsCenter = new Point(
                Canvas.GetLeft(rlsZone) + 85,
                Canvas.GetTop(rlsZone) + 30);

            if (Distance(playerCenter, footballCenter) < 90)
            {
                LaunchFootballGame();
                return;
            }

            if (Distance(playerCenter, mazeCenter) < 90)
            {
                LaunchMazeGame();
                return;
            }

            if (Distance(playerCenter, rlsCenter) < 90)
            {
                LaunchRls();
                return;
            }
        }

        private void LaunchFootballGame()
        {
            FootGame display = new FootGame();
            display.Show();
        }

        private void LaunchMazeGame()
        {
            Maze display = new Maze();
            display.Show();
        }

        private void LaunchRls()
        {
            RLS display = new RLS();
            display.Show();
        }

        private double Distance(Point a, Point b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        protected override void RunWhenWin()
        {
        }

        protected override void RunWhenLoose()
        {
        }
    }
}