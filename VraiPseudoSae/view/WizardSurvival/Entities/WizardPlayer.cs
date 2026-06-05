using System.Windows;
using VraiPseudoSae.Utils.Sprite;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Rendering;

namespace VraiPseudoSae.view.WizardSurvival.Entities;

/// <summary>
/// Player-controlled wizard. Movement and damage are kept separate from WPF input.
/// </summary>
public sealed class WizardPlayer : LivingWorldItem
{
    private enum AnimationMode
    {
        Idle,
        Walk,
        Death
    }

    private const double SpriteW = 32;
    private const double SpriteH = 32;
    private const double NormalSpeed = 210;
    private const double ImmuneSpeedBonus = 90;
    private const double IdleFrameDuration = 0.16;
    private const double WalkFrameDuration = 0.09;
    private const double DeathFrameDuration = 0.14;

    private readonly WizardPlayerSpriteSet sprites;
    private IReadOnlyList<string> currentFrames;
    private IReadOnlyList<string> deathFrames;
    private string renderedSprite;
    private AnimationMode animationMode;
    private double animationTimer;
    private int animationFrame;
    private bool isMoving;
    private double lakeSpeedMultiplier = 1;
    private double lakeRangeMultiplier = 1;
    private double lakeStatusRemaining;

    public WizardPlayer(double worldX, double worldY, WizardSurvivalGame game)
        : base(worldX, worldY, game, game.WizardSprites.InitialSprite, SpriteW, SpriteH, 13, 4, 30)
    {
        sprites = game.WizardSprites;
        currentFrames = sprites.IdleRight;
        deathFrames = sprites.DeathRightA;
        renderedSprite = sprites.InitialSprite;
        animationMode = AnimationMode.Idle;
        Facing = FacingDirection.Right;
        SetLocalCollisionBounds(7, 13, 16, 18);
    }

    public override string TypeName => "WizardPlayer";

    public FacingDirection Facing { get; private set; }

    public double CurrentSpeed => (NormalSpeed + (InvulnerabilityRemaining > 0 ? ImmuneSpeedBonus : 0)) * lakeSpeedMultiplier;

    public double RangeMultiplier => lakeRangeMultiplier;

    public LakeStatusKind LakeStatus { get; private set; }

    public double LakeStatusRemaining => lakeStatusRemaining;

    public bool DeathAnimationFinished { get; private set; }

    public void Move(DoubleVector input, double seconds, ICollisionMap map)
    {
        isMoving = input.Length > 0.01;

        if (input.X < 0)
            SetFacing(FacingDirection.Left);
        else if (input.X > 0)
            SetFacing(FacingDirection.Right);

        DoubleVector direction = input.Normalize();
        double terrainMultiplier = map.TerrainSpeedMultiplier(TerrainX, TerrainY);
        DoubleVector delta = direction * (CurrentSpeed * terrainMultiplier * seconds);
        Rect moved = MovementResolver.Move(CollisionBounds, delta, map);
        SetWorldPositionFromCollisionBounds(moved);
    }

    public void SetMovementIntent(bool moving)
    {
        isMoving = moving;
    }

    public override bool TakeDamage(DamageRequest damage)
    {
        if (!CanTakeDamage)
            return false;

        Health = System.Math.Max(0, Health - System.Math.Max(0, damage.Amount));
        if (Health > 0)
            InvulnerabilityRemaining = 2.0;

        return true;
    }

    public void HealOne() => Heal(1);

    public void Heal(int amount)
    {
        if (amount > 0 && Health < MaxHealth)
            Health = System.Math.Min(MaxHealth, Health + amount);
    }

    public void ApplyLakeStatus(LakeStatusKind status, double speedMultiplier, double rangeMultiplier, double duration)
    {
        LakeStatus = status;
        lakeSpeedMultiplier = speedMultiplier;
        lakeRangeMultiplier = rangeMultiplier;
        lakeStatusRemaining = duration;
    }

    public bool TakeEnvironmentalDamage(DamageRequest damage)
    {
        if (Health <= 0)
            return false;

        Health = System.Math.Max(0, Health - System.Math.Max(0, damage.Amount));
        InvulnerabilityRemaining = System.Math.Max(InvulnerabilityRemaining, 0.7);
        return true;
    }

    public void StartDeathAnimation(bool alternateRow)
    {
        if (animationMode == AnimationMode.Death)
            return;

        animationMode = AnimationMode.Death;
        deathFrames = alternateRow
            ? (Facing == FacingDirection.Right ? sprites.DeathRightB : sprites.DeathLeftB)
            : (Facing == FacingDirection.Right ? sprites.DeathRightA : sprites.DeathLeftA);
        animationFrame = 0;
        animationTimer = 0;
        isMoving = false;
        DeathAnimationFinished = false;
        ApplyFrame(deathFrames[animationFrame], force: true);
    }

    public override void TickLiving(double seconds)
    {
        base.TickLiving(seconds);
        if (lakeStatusRemaining > 0)
        {
            lakeStatusRemaining = System.Math.Max(0, lakeStatusRemaining - seconds);
            if (lakeStatusRemaining <= 0)
            {
                LakeStatus = LakeStatusKind.None;
                lakeSpeedMultiplier = 1;
                lakeRangeMultiplier = 1;
            }
        }

        TickAnimation(seconds);
        SyncScreenPosition();
    }

    private void SetFacing(FacingDirection facing)
    {
        if (animationMode == AnimationMode.Death)
            return;

        if (Facing == facing)
            return;

        Facing = facing;
        ForceAnimationFrame();
    }

    private void TickAnimation(double seconds)
    {
        if (animationMode == AnimationMode.Death)
        {
            TickDeathAnimation(seconds);
            return;
        }

        AnimationMode nextMode = isMoving ? AnimationMode.Walk : AnimationMode.Idle;
        IReadOnlyList<string> nextFrames = GetCurrentDirectionalFrames(nextMode);
        double frameDuration = nextMode == AnimationMode.Walk ? WalkFrameDuration : IdleFrameDuration;

        if (animationMode != nextMode || !ReferenceEquals(currentFrames, nextFrames))
        {
            animationMode = nextMode;
            currentFrames = nextFrames;
            animationFrame = 0;
            animationTimer = 0;
            ApplyFrame(currentFrames[animationFrame], force: true);
            return;
        }

        animationTimer += seconds;
        while (animationTimer >= frameDuration)
        {
            animationTimer -= frameDuration;
            animationFrame = (animationFrame + 1) % currentFrames.Count;
            ApplyFrame(currentFrames[animationFrame]);
        }
    }

    private void TickDeathAnimation(double seconds)
    {
        if (DeathAnimationFinished)
            return;

        animationTimer += seconds;
        while (animationTimer >= DeathFrameDuration && !DeathAnimationFinished)
        {
            animationTimer -= DeathFrameDuration;
            if (animationFrame < deathFrames.Count - 1)
            {
                animationFrame++;
                ApplyFrame(deathFrames[animationFrame]);
            }
            else
            {
                DeathAnimationFinished = true;
            }
        }
    }

    private IReadOnlyList<string> GetCurrentDirectionalFrames(AnimationMode mode)
    {
        bool right = Facing == FacingDirection.Right;
        return mode == AnimationMode.Walk
            ? right ? sprites.WalkRight : sprites.WalkLeft
            : right ? sprites.IdleRight : sprites.IdleLeft;
    }

    private void ForceAnimationFrame()
    {
        if (animationMode == AnimationMode.Death)
            return;

        currentFrames = GetCurrentDirectionalFrames(animationMode);
        animationFrame = System.Math.Min(animationFrame, currentFrames.Count - 1);
        ApplyFrame(currentFrames[animationFrame], force: true);
    }

    private void ApplyFrame(string spriteName, bool force = false)
    {
        if (!force && renderedSprite == spriteName)
            return;

        renderedSprite = spriteName;
        SpriteFrameSwitcher.SwitchFrame(this, spriteName);
    }

    public override void CollideEffect(IUTGame.GameItem other)
    {
    }
}
