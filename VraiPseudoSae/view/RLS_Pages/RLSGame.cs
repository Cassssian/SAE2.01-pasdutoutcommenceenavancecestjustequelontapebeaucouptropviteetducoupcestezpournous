using System;
using System.Collections.Generic;
using System.Windows.Input;
using IUTGame;
using VraiPseudoSae.data.AudioPlayer;

namespace VraiPseudoSae.view.RLS_Pages
{
    public class RLSGame : Game
    {
        public const double Gravity = 0.6;
        public const double GroundY = 520;
        public const double FieldWidth = 1000;
        public const double Friction = 0.92;
        public const double BallFriction = 0.99;
        public const double BallBounce = 0.75;

        internal readonly JsonPakAudioService Audio;

        internal RLSCar Car1 = null!;
        internal RLSCar Car2 = null!;
        internal RLSBall Ball = null!;

        internal readonly HashSet<Key> Keys = new();
        internal bool GoalPaused;
        internal int GoalTimer;

        public bool VsBot { get; }

        public int Score1 { get; internal set; }
        public int Score2 { get; internal set; }

        public Action? OnHudRefresh;
        public Action<string>? OnGoalShown;
        public Action? OnGoalHidden;
        public Action? OnBackToMenu;

        public RLSGame(
            IScreen screen,
            string spritesFolder,
            string soundsFolder,
            JsonPakAudioService audio,
            bool vsBot)
            : base(screen, spritesFolder, soundsFolder, 60)
        {
            Audio = audio;
            VsBot = vsBot;
        }

        protected override void InitItems()
        {
            AddItem(new RLSFloor(0, 520, this, false));
            AddItem(new RLSFloor(500, 520, this, true));

            AddItem(new RLSGoal(0, 365, this, false));
            AddItem(new RLSGoal(974, 365, this, true));

            Car1 = new RLSCar(150, 482, this, "rls_car1.png");
            Car2 = new RLSCar(790, 482, this, "rls_car2.png");
            Ball = new RLSBall(485, 250, this);

            AddItem(Car1);
            AddItem(Car2);
            AddItem(Ball);
            AddItem(new RLSController(this));

            ResetScore();
            ResetPositions();
            OnHudRefresh?.Invoke();
        }

        public void ResetPositions()
        {
            Car1.VX = 0;
            Car1.VY = 0;
            Car1.OnGround = true;
            Car1.JumpsLeft = 2;
            Car1.JumpKeyPrev = false;
            Car1.Boost = 100;
            Car1.FacingDir = 1;
            Car1.PutPosition(150, 482);
            Car1.ApplyFlip();

            Car2.VX = 0;
            Car2.VY = 0;
            Car2.OnGround = true;
            Car2.JumpsLeft = 2;
            Car2.JumpKeyPrev = false;
            Car2.Boost = 100;
            Car2.FacingDir = -1;
            Car2.PutPosition(790, 482);
            Car2.ApplyFlip();

            Ball.VX = 0;
            Ball.VY = 0;
            Ball.PutPosition(485, 250);

            OnHudRefresh?.Invoke();
        }

        public void ResetScore()
        {
            Score1 = 0;
            Score2 = 0;
            OnHudRefresh?.Invoke();
        }

        public void ShowGoal(string message)
        {
            GoalPaused = true;
            GoalTimer = 90;
            OnGoalShown?.Invoke(message);
        }

        public void EndGoalPause()
        {
            GoalPaused = false;
            OnGoalHidden?.Invoke();
            ResetPositions();
        }

        protected override void RunWhenWin() { }

        protected override void RunWhenLoose() { }
    }
}