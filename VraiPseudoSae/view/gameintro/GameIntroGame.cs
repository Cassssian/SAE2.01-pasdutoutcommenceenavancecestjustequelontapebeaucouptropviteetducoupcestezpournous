using System;
using System.Threading.Tasks;
using System.Windows;
using IUTGame;

namespace VraiPseudoSae.view.gameintro;

public sealed class GameIntroGame : Game
{
    public const double MapSize = 128;

    private readonly GameIntroPlayerSpriteSet playerSprites;

    public GameIntroGame(IScreen screen, GameIntroPlayerSpriteSet playerSprites)
        : base(screen, "Resources/Sprites", "__iutgame_intro_no_audio__", 60)
    {
        this.playerSprites = playerSprites;
    }

    public GameIntroPlayer Player { get; private set; } = null!;

    public Point PlayerCenter => Player.Center;

    protected override void InitItems()
    {
        Player = new GameIntroPlayer(96, 72, this, playerSprites);
        AddItem(Player);
    }

    public void SetPlayerInput(double dx, double dy) => Player.SetManualMovement(dx, dy);

    public void StopPlayerMovement() => Player.StopMovement();

    public Task MovePlayerToAsync(double left, double top, int durationMilliseconds) =>
        Player.MoveToAsync(left, top, durationMilliseconds);

    public void FacePlayerDown() => Player.FaceDown();

    public void FacePlayerTowards(Point target) => Player.FaceTowards(target);

    public Task RunPlayerCirclesAsync(Point center, double radius, int loops, int durationMilliseconds) =>
        Player.RunCirclesAsync(center, radius, loops, durationMilliseconds);

    public Task PlayPlayerAttackAsync(Point target, Action<int>? frameChanged = null) =>
        Player.PlayActionTowardsAsync(GameIntroCharacterAnimation.Attack, target, frameChanged);

    public Task PlayPlayerInteractAsync(Point target) =>
        Player.PlayActionTowardsAsync(GameIntroCharacterAnimation.Interact, target);

    protected override void RunWhenWin()
    {
    }

    protected override void RunWhenLoose()
    {
    }
}
