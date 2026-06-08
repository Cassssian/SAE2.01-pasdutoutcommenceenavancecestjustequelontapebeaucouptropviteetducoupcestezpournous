using System.Collections.Generic;

namespace VraiPseudoSae.view.gameintro;

public sealed class GameIntroPlayerSpriteSet
{
    private readonly Dictionary<GameIntroCharacterPose, IReadOnlyList<string>> frames = new();

    public string InitialSprite => GetFrames(GameIntroCharacterAnimation.Idle, GameIntroCharacterDirection.Down, false)[0];

    public void AddFrames(
        GameIntroCharacterAnimation animation,
        GameIntroCharacterDirection direction,
        bool mirrored,
        IReadOnlyList<string> spriteNames)
    {
        frames[new GameIntroCharacterPose(animation, direction, mirrored)] = spriteNames;
    }

    public IReadOnlyList<string> GetFrames(
        GameIntroCharacterAnimation animation,
        GameIntroCharacterDirection direction,
        bool mirrored)
    {
        return frames[new GameIntroCharacterPose(animation, direction, mirrored)];
    }

    private readonly record struct GameIntroCharacterPose(
        GameIntroCharacterAnimation Animation,
        GameIntroCharacterDirection Direction,
        bool Mirrored);
}

public enum GameIntroCharacterAnimation
{
    Idle,
    Walk,
    Run,
    Attack,
    Interact
}

public enum GameIntroCharacterDirection
{
    Down = 0,
    DownSide = 1,
    Right = 2,
    UpSide = 3,
    Up = 4
}
