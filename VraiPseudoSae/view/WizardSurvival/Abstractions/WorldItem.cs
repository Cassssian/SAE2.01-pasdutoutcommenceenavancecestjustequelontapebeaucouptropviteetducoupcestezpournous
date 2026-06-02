using System.Windows;
using IUTGame;
using VraiPseudoSae.view.WizardSurvival.Core;

namespace VraiPseudoSae.view.WizardSurvival.Abstractions;

/// <summary>
/// Base class for every visible arena object backed by an IUTGame sprite.
/// </summary>
public abstract class WorldItem : GameItem, IWorldObject
{
    private readonly double spriteWidth;
    private readonly double spriteHeight;

    protected WorldItem(
        double worldX,
        double worldY,
        WizardSurvivalGame game,
        string spriteName,
        double spriteWidth,
        double spriteHeight,
        double collisionRadius,
        int zIndex)
        : base(worldX, worldY, game, spriteName, zIndex)
    {
        this.spriteWidth = spriteWidth;
        this.spriteHeight = spriteHeight;
        CollisionRadius = collisionRadius;
        WorldX = worldX;
        WorldY = worldY;
        IsActive = true;
        Collidable = false;
        SyncScreenPosition();
    }

    protected WizardSurvivalGame Game => (WizardSurvivalGame)TheGame;

    public double WorldX { get; private set; }

    public double WorldY { get; private set; }

    public new double Width => spriteWidth;

    public new double Height => spriteHeight;

    public double CollisionRadius { get; protected set; }

    public bool IsActive { get; protected set; }

    public double CenterX => WorldX + Width / 2.0;

    public double CenterY => WorldY + Height / 2.0;

    public Rect Bounds => new(WorldX, WorldY, Width, Height);

    public void SetWorldPosition(double worldX, double worldY)
    {
        WorldX = worldX;
        WorldY = worldY;
        SyncScreenPosition();
    }

    public void MoveWorld(double dx, double dy) => SetWorldPosition(WorldX + dx, WorldY + dy);

    public void SyncScreenPosition() => PutXY(WorldX - Game.Camera.X, WorldY - Game.Camera.Y);

    public double DistanceTo(IWorldObject other)
    {
        double dx = CenterX - other.CenterX;
        double dy = CenterY - other.CenterY;
        return System.Math.Sqrt(dx * dx + dy * dy);
    }

    public bool CircleIntersects(IWorldObject other) =>
        DistanceTo(other) <= CollisionRadius + other.CollisionRadius;

    public void Deactivate() => IsActive = false;

    public override bool IsCollide(GameItem other)
    {
        return other is IWorldObject worldObject
            ? CircleIntersects(worldObject)
            : base.IsCollide(other);
    }
}
