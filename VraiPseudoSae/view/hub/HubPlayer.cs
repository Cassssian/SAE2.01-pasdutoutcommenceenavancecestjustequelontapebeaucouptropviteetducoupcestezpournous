using System;
using System.Windows;
using System.Windows.Input;
using IUTGame;

namespace VraiPseudoSae.view.hub
{
    public class HubPlayer : GameItem, IAnimable, IKeyboardInteract
    {
        private bool goUp;
        private bool goDown;
        private bool goLeft;
        private bool goRight;

        private const double BaseSpeed = 4.0;

        public HubPlayer(double x, double y, Game game)
            : base(x, y, game, "player_hub.png", 520)
        {
            Collidable = false;
            ApplyFake3DScale();
        }

        public override string TypeName => "HubPlayer";

        public Point Center => new Point(Left + Width / 2.0, Top + Height / 2.0);

        public void Animate(TimeSpan interval)
        {
            if (goUp) MoveXY(0, -BaseSpeed);
            if (goDown) MoveXY(0, BaseSpeed);
            if (goLeft) MoveXY(-BaseSpeed, 0);
            if (goRight) MoveXY(BaseSpeed, 0);

            ClampToHub();
            ApplyFake3DScale();

            if (TheGame is HubGame hubGame)
                hubGame.UpdateInfoText();
        }

        private void ClampToHub()
        {
            if (Left < 0) Left = 0;
            if (Top < 250) Top = 250;
            if (Left > 1220) Left = 1220;
            if (Top > 610) Top = 610;
        }

        private void ApplyFake3DScale()
        {
            double t = (Top - 250) / (610 - 250);
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            double scale = 0.72 + (1.18 - 0.72) * t;
            ChangeScale(scale, scale);
            ZIndex = (int)Top;
        }

        public void KeyDown(Key key)
        {
            if (key == Key.Z || key == Key.W || key == Key.Up) goUp = true;
            if (key == Key.S || key == Key.Down) goDown = true;
            if (key == Key.Q || key == Key.A || key == Key.Left) goLeft = true;
            if (key == Key.D || key == Key.Right) goRight = true;

            if (key == Key.E && TheGame is HubGame hubGame)
                hubGame.TryLaunchMiniGame();
        }

        public void KeyUp(Key key)
        {
            if (key == Key.Z || key == Key.W) goUp = false;
            if (key == Key.S) goDown = false;
            if (key == Key.Q || key == Key.A) goLeft = false;
            if (key == Key.D) goRight = false;
        }

        public override void CollideEffect(GameItem other) { }
    }
}
