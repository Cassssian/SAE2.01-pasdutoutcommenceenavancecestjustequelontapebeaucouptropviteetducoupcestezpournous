using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using IUTGame;
using VraiPseudoSae.Utils.Sprite;

namespace VraiPseudoSae.view.gameintro;

public sealed class GameIntroPlayer : GameItem, IAnimable
{
    public const double SpriteWidth = 16;
    public const double SpriteHeight = 24;
    public const double Speed = 30;

    private const double IdleFrameDuration = 0.18;
    private const double WalkFrameDuration = 0.14;
    private const double RunFrameDuration = 0.08;
    private const double ActionFrameDuration = 0.095;

    private readonly GameIntroPlayerSpriteSet sprites;
    private GameIntroCharacterAnimation currentAnimation = GameIntroCharacterAnimation.Idle;
    private GameIntroCharacterDirection currentDirection = GameIntroCharacterDirection.Down;
    private string renderedSprite;
    private double worldLeft;
    private double worldTop;
    private double inputX;
    private double inputY;
    private double frameTimer;
    private int frame;
    private bool mirrored;
    private bool isMoving;
    private GameIntroCharacterAnimation movementAnimation = GameIntroCharacterAnimation.Walk;
    private AutoMove? autoMove;
    private CircleRun? circleRun;
    private OneShotAnimation? oneShotAnimation;

    public GameIntroPlayer(double left, double top, Game game, GameIntroPlayerSpriteSet sprites)
        : base(left, top, game, sprites.InitialSprite, 90)
    {
        this.sprites = sprites;
        renderedSprite = sprites.InitialSprite;
        worldLeft = left;
        worldTop = top;
        Collidable = false;
        SyncScreenPosition();
    }

    public override string TypeName => "GameIntroPlayer";

    public double WorldLeft => worldLeft;

    public double WorldTop => worldTop;

    public Point Center => new(worldLeft + SpriteWidth / 2.0, worldTop + SpriteHeight / 2.0);

    public void Animate(TimeSpan dt)
    {
        double seconds = Math.Min(0.05, dt.TotalSeconds);

        if (oneShotAnimation is not null)
        {
            TickOneShotAnimation(seconds);
            SyncScreenPosition();
            return;
        }

        if (circleRun is not null)
            TickCircleRun(seconds);
        else if (autoMove is not null)
            TickAutoMove(seconds);
        else
            TickManualMovement(seconds);

        TickAnimation(seconds);
        SyncScreenPosition();
    }

    public void SetManualMovement(double dx, double dy)
    {
        inputX = dx;
        inputY = dy;
    }

    public void StopMovement()
    {
        inputX = 0;
        inputY = 0;
        autoMove?.Completion.TrySetCanceled();
        circleRun?.Completion.TrySetCanceled();
        autoMove = null;
        circleRun = null;
        SetMoving(false);
    }

    public void SetWorldPosition(double left, double top)
    {
        worldLeft = Clamp(left, 0, GameIntroGame.MapSize - SpriteWidth);
        worldTop = Clamp(top, 0, GameIntroGame.MapSize - SpriteHeight);
        SyncScreenPosition();
    }

    public Task MoveToAsync(double targetLeft, double targetTop, int durationMilliseconds)
    {
        inputX = 0;
        inputY = 0;

        double clampedTargetLeft = Clamp(targetLeft, 0, GameIntroGame.MapSize - SpriteWidth);
        double clampedTargetTop = Clamp(targetTop, 0, GameIntroGame.MapSize - SpriteHeight);

        if (durationMilliseconds <= 0 ||
            Distance(new Point(worldLeft, worldTop), new Point(clampedTargetLeft, clampedTargetTop)) < 0.001)
        {
            SetWorldPosition(clampedTargetLeft, clampedTargetTop);
            SetMoving(false);
            return Task.CompletedTask;
        }

        autoMove?.Completion.TrySetCanceled();
        circleRun?.Completion.TrySetCanceled();
        circleRun = null;
        autoMove = new AutoMove(
            worldLeft,
            worldTop,
            clampedTargetLeft,
            clampedTargetTop,
            Math.Max(0.001, durationMilliseconds / 1000.0),
            new TaskCompletionSource());

        SetDirectionFromVector(clampedTargetLeft - worldLeft, clampedTargetTop - worldTop);
        movementAnimation = GameIntroCharacterAnimation.Walk;
        SetMoving(true);
        return autoMove.Completion.Task;
    }

    public void FaceDown()
    {
        StopMovement();
        SetDirectionFromVector(0, 1);
        ApplyCurrentFrame(force: true);
    }

    public void FaceTowards(Point target)
    {
        StopMovement();
        SetDirectionFromVector(target.X - Center.X, target.Y - Center.Y);
        ApplyCurrentFrame(force: true);
    }

    public Task RunCirclesAsync(Point center, double radius, int loops, int durationMilliseconds)
    {
        inputX = 0;
        inputY = 0;
        autoMove?.Completion.TrySetCanceled();
        autoMove = null;

        double startAngle = Math.Atan2(Center.Y - center.Y, Center.X - center.X);
        if (double.IsNaN(startAngle))
            startAngle = 0;

        circleRun?.Completion.TrySetCanceled();
        circleRun = new CircleRun(
            center,
            radius,
            Math.Max(1, loops),
            Math.Max(0.001, durationMilliseconds / 1000.0),
            startAngle,
            new TaskCompletionSource());

        movementAnimation = GameIntroCharacterAnimation.Run;
        SetMoving(true);
        return circleRun.Completion.Task;
    }

    public Task PlayActionTowardsAsync(
        GameIntroCharacterAnimation animation,
        Point target,
        Action<int>? frameChanged = null)
    {
        if (animation is not GameIntroCharacterAnimation.Attack and not GameIntroCharacterAnimation.Interact)
            throw new ArgumentException("Only attack and interaction animations can be played as one-shot actions.", nameof(animation));

        StopMovement();
        SetDirectionFromVector(target.X - Center.X, target.Y - Center.Y);
        currentAnimation = animation;
        frame = 0;
        frameTimer = 0;
        isMoving = false;
        oneShotAnimation = new OneShotAnimation(animation, frameChanged, new TaskCompletionSource());
        ApplyCurrentFrame(force: true);
        frameChanged?.Invoke(frame);
        return oneShotAnimation.Completion.Task;
    }

    private void TickManualMovement(double seconds)
    {
        double dx = inputX;
        double dy = inputY;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
        {
            SetMoving(false);
            return;
        }

        double length = Math.Sqrt(dx * dx + dy * dy);
        dx /= length;
        dy /= length;
        SetDirectionFromVector(dx, dy);
        movementAnimation = GameIntroCharacterAnimation.Walk;
        SetMoving(true);
        SetWorldPosition(worldLeft + dx * Speed * seconds, worldTop + dy * Speed * seconds);
    }

    private void TickAutoMove(double seconds)
    {
        AutoMove move = autoMove!;
        move.Elapsed += seconds;
        double progress = Clamp(move.Elapsed / move.Duration, 0, 1);
        double eased = 0.5 - Math.Cos(progress * Math.PI) / 2.0;

        SetWorldPosition(
            Lerp(move.StartLeft, move.TargetLeft, eased),
            Lerp(move.StartTop, move.TargetTop, eased));

        if (progress < 1)
            return;

        autoMove = null;
        SetMoving(false);
        move.Completion.TrySetResult();
    }

    private void TickCircleRun(double seconds)
    {
        CircleRun run = circleRun!;
        run.Elapsed += seconds;
        double progress = Clamp(run.Elapsed / run.Duration, 0, 1);
        double angle = run.StartAngle + progress * Math.PI * 2.0 * run.Loops;
        double previousX = Center.X;
        double previousY = Center.Y;
        double nextCenterX = run.Center.X + Math.Cos(angle) * run.Radius;
        double nextCenterY = run.Center.Y + Math.Sin(angle) * run.Radius;

        SetWorldPosition(nextCenterX - SpriteWidth / 2.0, nextCenterY - SpriteHeight / 2.0);
        SetDirectionFromVector(nextCenterX - previousX, nextCenterY - previousY);
        movementAnimation = GameIntroCharacterAnimation.Run;
        SetMoving(true);

        if (progress < 1)
            return;

        circleRun = null;
        SetMoving(false);
        run.Completion.TrySetResult();
    }

    private void TickAnimation(double seconds)
    {
        GameIntroCharacterAnimation nextAnimation = isMoving
            ? movementAnimation
            : GameIntroCharacterAnimation.Idle;

        double frameDuration = nextAnimation switch
        {
            GameIntroCharacterAnimation.Run => RunFrameDuration,
            GameIntroCharacterAnimation.Walk => WalkFrameDuration,
            _ => IdleFrameDuration
        };

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

    private void TickOneShotAnimation(double seconds)
    {
        OneShotAnimation oneShot = oneShotAnimation!;
        IReadOnlyList<string> frames = sprites.GetFrames(oneShot.Animation, currentDirection, mirrored);

        frameTimer += seconds;
        while (frameTimer >= ActionFrameDuration && oneShotAnimation is not null)
        {
            frameTimer -= ActionFrameDuration;
            if (frame < frames.Count - 1)
            {
                frame++;
                ApplyCurrentFrame(force: true);
                oneShot.FrameChanged?.Invoke(frame);
            }
            else
            {
                oneShotAnimation = null;
                currentAnimation = GameIntroCharacterAnimation.Idle;
                frame = 0;
                frameTimer = 0;
                SetMoving(false);
                ApplyCurrentFrame(force: true);
                oneShot.Completion.TrySetResult();
            }
        }
    }

    private void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    private void SetDirectionFromVector(double dx, double dy)
    {
        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return;

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

    private void SyncScreenPosition()
    {
        PutXY(worldLeft, worldTop);
        int zIndex = (int)(worldTop + SpriteHeight);

        if (ZIndex != zIndex)
            ZIndex = zIndex;
    }

    public override void CollideEffect(GameItem other)
    {
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
    }

    private static double Lerp(double start, double end, double progress) => start + (end - start) * progress;

    private static double Distance(Point a, Point b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private sealed class AutoMove
    {
        public AutoMove(
            double startLeft,
            double startTop,
            double targetLeft,
            double targetTop,
            double duration,
            TaskCompletionSource completion)
        {
            StartLeft = startLeft;
            StartTop = startTop;
            TargetLeft = targetLeft;
            TargetTop = targetTop;
            Duration = duration;
            Completion = completion;
        }

        public double StartLeft { get; }

        public double StartTop { get; }

        public double TargetLeft { get; }

        public double TargetTop { get; }

        public double Duration { get; }

        public double Elapsed { get; set; }

        public TaskCompletionSource Completion { get; }
    }

    private sealed class CircleRun
    {
        public CircleRun(
            Point center,
            double radius,
            int loops,
            double duration,
            double startAngle,
            TaskCompletionSource completion)
        {
            Center = center;
            Radius = radius;
            Loops = loops;
            Duration = duration;
            StartAngle = startAngle;
            Completion = completion;
        }

        public Point Center { get; }

        public double Radius { get; }

        public int Loops { get; }

        public double Duration { get; }

        public double StartAngle { get; }

        public double Elapsed { get; set; }

        public TaskCompletionSource Completion { get; }
    }

    private sealed class OneShotAnimation
    {
        public OneShotAnimation(
            GameIntroCharacterAnimation animation,
            Action<int>? frameChanged,
            TaskCompletionSource completion)
        {
            Animation = animation;
            FrameChanged = frameChanged;
            Completion = completion;
        }

        public GameIntroCharacterAnimation Animation { get; }

        public Action<int>? FrameChanged { get; }

        public TaskCompletionSource Completion { get; }
    }
}
