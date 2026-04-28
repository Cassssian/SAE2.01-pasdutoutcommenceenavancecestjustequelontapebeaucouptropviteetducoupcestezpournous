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
            double speed = BaseSpeed;

            if (goUp) MoveXY(0, -speed);
            if (goDown) MoveXY(0, speed);
            if (goLeft) MoveXY(-speed, 0);
            if (goRight) MoveXY(speed, 0);

            ClampToHub();
            ApplyFake3DScale();

            if (TheGame is HubGame hubGame)
                hubGame.UpdateInfoText();
        }

        private void ClampToHub()
        {
            if (Left < 0)
                Left = 0;

            if (Top < 250)
                Top = 250;

            if (Left > 1220)
                Left = 1220;

            if (Top > 610)
                Top = 610;
        }

        private void ApplyFake3DScale()
        {
            double minScale = 0.72;
            double maxScale = 1.18;

            double t = (Top - 250) / (610 - 250);

            if (t < 0) t = 0;
            if (t > 1) t = 1;

            double scale = minScale + (maxScale - minScale) * t;

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
            if (key == Key.Z || key == Key.W || key == Key.Up) goUp = false;
            if (key == Key.S || key == Key.Down) goDown = false;
            if (key == Key.Q || key == Key.A || key == Key.Left) goLeft = false;
            if (key == Key.D || key == Key.Right) goRight = false;
        }

        public override void CollideEffect(GameItem other)
        {
        }
    }
}