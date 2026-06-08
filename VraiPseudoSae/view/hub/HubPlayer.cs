using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using IUTGame;
using VraiPseudoSae.Utils.SaveManager;
using VraiPseudoSae.Utils.Sprite;
using VraiPseudoSae.view.gameintro;

namespace VraiPseudoSae.view.hub
{
    public class HubPlayer : GameItem, IAnimable, IKeyboardInteract
    {
        private const double HubSpriteScale = 1.0;
        private const double MovementSpeed = 90.0;
        private const double MinTop = 0.0;
        private const double IdleFrameDuration = 0.18;
        private const double WalkFrameDuration = 0.14;
        private const double InteractionContactMargin = 3.0;
        private const double CollisionWidthRatio = 0.62;
        private const double CollisionHeightRatio = 0.32;

        private readonly GameIntroPlayerSpriteSet sprites;
        private bool goUp;
        private bool goDown;
        private bool goLeft;
        private bool goRight;
        private GameIntroCharacterAnimation currentAnimation = GameIntroCharacterAnimation.Idle;
        private GameIntroCharacterDirection currentDirection = GameIntroCharacterDirection.Down;
        private string renderedSprite;
        private double frameTimer;
        private int frame;
        private bool mirrored;
        private bool inputLocked;
        private ParametresJeuSauvegarde settings = ParametresJeuSauvegarde.ParDefaut;

        public HubPlayer(double x, double y, Game game, GameIntroPlayerSpriteSet sprites)
            : base(x, y, game, sprites.InitialSprite, 520)
        {
            this.sprites = sprites;
            renderedSprite = sprites.InitialSprite;
            Collidable = false;
            ChangeScale(HubSpriteScale, HubSpriteScale);
            PutXY(x, y);
            ApplyZIndex();
        }

        public override string TypeName => "HubPlayer";

        public Point Center => new Point(Left + Width / 2.0, Top + Height / 2.0);

        public Rect CollisionBounds => GetCollisionBounds(Left, Top);

        public Rect InteractionBounds => Expand(CollisionBounds, InteractionContactMargin);

        public void ApplySettings(ParametresJeuSauvegarde nextSettings)
        {
            settings = nextSettings.Normaliser();
            ResetMovementInput();
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;

            if (locked)
                ResetMovementInput();
        }

        public void Animate(TimeSpan interval)
        {
            double seconds = interval.TotalSeconds > 0
                ? Math.Min(0.05, interval.TotalSeconds)
                : 1.0 / 60.0;

            double dx = 0;
            double dy = 0;

            if (goUp) dy -= 1;
            if (goDown) dy += 1;
            if (goLeft) dx -= 1;
            if (goRight) dx += 1;

            bool moving = Math.Abs(dx) > 0.001 || Math.Abs(dy) > 0.001;

            if (moving)
            {
                double length = Math.Sqrt(dx * dx + dy * dy);
                dx /= length;
                dy /= length;
                SetDirectionFromVector(dx, dy);
                MoveWithCollisions(dx * MovementSpeed * seconds, dy * MovementSpeed * seconds);
            }

            ClampToHub();
            TickAnimation(seconds, moving);
            ApplyZIndex();

            if (TheGame is HubGame hubGame)
                hubGame.UpdateInfoText();
        }

        private void MoveWithCollisions(double dx, double dy)
        {
            if (TheGame is not HubGame hubGame)
            {
                MoveXY(dx, dy);
                return;
            }

            if (Math.Abs(dx) > 0.001 && CanOccupy(Left + dx, Top, hubGame))
                MoveXY(dx, 0);

            if (Math.Abs(dy) > 0.001 && CanOccupy(Left, Top + dy, hubGame))
                MoveXY(0, dy);
        }

        private bool CanOccupy(double left, double top, HubGame hubGame)
        {
            return !hubGame.IsBlockedByLaunchZone(GetCollisionBounds(left, top));
        }

        private Rect GetCollisionBounds(double left, double top)
        {
            double collisionWidth = Math.Max(6.0, Width * CollisionWidthRatio);
            double collisionHeight = Math.Max(4.0, Height * CollisionHeightRatio);
            double collisionLeft = left + (Width - collisionWidth) / 2.0;
            double collisionTop = top + Height - collisionHeight;

            return new Rect(collisionLeft, collisionTop, collisionWidth, collisionHeight);
        }

        private static Rect Expand(Rect bounds, double margin)
        {
            return new Rect(
                bounds.Left - margin,
                bounds.Top - margin,
                bounds.Width + margin * 2,
                bounds.Height + margin * 2);
        }

        private void ClampToHub()
        {
            double gameWidth = GameWidth > 0 ? GameWidth : 1280;
            double gameHeight = GameHeight > 0 ? GameHeight : 720;
            double maxLeft = Math.Max(0, gameWidth - Width);
            double maxTop = Math.Max(MinTop, gameHeight - Height);

            if (Left < 0) Left = 0;
            if (Top < MinTop) Top = MinTop;
            if (Left > maxLeft) Left = maxLeft;
            if (Top > maxTop) Top = maxTop;
        }

        private void TickAnimation(double seconds, bool moving)
        {
            GameIntroCharacterAnimation nextAnimation = moving
                ? GameIntroCharacterAnimation.Walk
                : GameIntroCharacterAnimation.Idle;
            double frameDuration = moving ? WalkFrameDuration : IdleFrameDuration;

            if (currentAnimation != nextAnimation)
            {
                currentAnimation = nextAnimation;
                frame = 0;
                frameTimer = 0;
                ApplyCurrentFrame(force: true);
                return;
            }

            frameTimer += seconds;
            IReadOnlyList<string> currentFrames = sprites.GetFrames(currentAnimation, currentDirection, mirrored);

            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frame = (frame + 1) % currentFrames.Count;
                ApplyCurrentFrame();
            }
        }

        private void SetDirectionFromVector(double dx, double dy)
        {
            double absX = Math.Abs(dx);
            double absY = Math.Abs(dy);
            GameIntroCharacterDirection nextDirection;

            if (absX < absY * 0.35)
            {
                nextDirection = dy < 0
                    ? GameIntroCharacterDirection.Up
                    : GameIntroCharacterDirection.Down;
            }
            else if (absY < absX * 0.35)
            {
                nextDirection = GameIntroCharacterDirection.Right;
            }
            else
            {
                nextDirection = dy < 0
                    ? GameIntroCharacterDirection.UpSide
                    : GameIntroCharacterDirection.DownSide;
            }

            bool nextMirrored = nextDirection is GameIntroCharacterDirection.Right
                                    or GameIntroCharacterDirection.DownSide
                                    or GameIntroCharacterDirection.UpSide
                                && dx < -0.001;

            if (currentDirection == nextDirection && mirrored == nextMirrored)
                return;

            currentDirection = nextDirection;
            mirrored = nextMirrored;
            frame = 0;
            frameTimer = 0;
            ApplyCurrentFrame(force: true);
        }

        private void ApplyCurrentFrame(bool force = false)
        {
            IReadOnlyList<string> currentFrames = sprites.GetFrames(currentAnimation, currentDirection, mirrored);
            string spriteName = currentFrames[frame % currentFrames.Count];

            if (!force && renderedSprite == spriteName)
                return;

            renderedSprite = spriteName;
            SpriteFrameSwitcher.SwitchFrame(this, spriteName);
        }

        private void ApplyZIndex()
        {
            ZIndex = (int)Bottom;
        }

        public void KeyDown(Key key)
        {
            if (inputLocked)
                return;

            if (key == ForwardKey) goUp = true;
            if (key == BackwardKey) goDown = true;
            if (key == LeftKey) goLeft = true;
            if (key == RightKey) goRight = true;

            if (key == InteractionKey && TheGame is HubGame hubGame)
                hubGame.TryLaunchMiniGame();
        }

        public void KeyUp(Key key)
        {
            if (key == ForwardKey) goUp = false;
            if (key == BackwardKey) goDown = false;
            if (key == LeftKey) goLeft = false;
            if (key == RightKey) goRight = false;
        }

        private void ResetMovementInput()
        {
            goUp = false;
            goDown = false;
            goLeft = false;
            goRight = false;
        }

        private Key ForwardKey => HomePage.ParseSavedKey(settings.ToucheAvancer, Key.Z);

        private Key BackwardKey => HomePage.ParseSavedKey(settings.ToucheReculer, Key.S);

        private Key LeftKey => HomePage.ParseSavedKey(settings.ToucheGauche, Key.Q);

        private Key RightKey => HomePage.ParseSavedKey(settings.ToucheDroite, Key.D);

        private Key InteractionKey => HomePage.ParseSavedKey(settings.ToucheInteraction, Key.E);

        public override void CollideEffect(GameItem other) { }
    }
}
