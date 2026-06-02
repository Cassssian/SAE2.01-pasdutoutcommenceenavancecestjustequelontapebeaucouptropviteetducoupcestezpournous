using System;
using System.Collections.Generic;
using System.Linq;
using IUTGame;
using VraiPseudoSae.view.WizardSurvival.Abstractions;
using VraiPseudoSae.view.WizardSurvival.Core;
using VraiPseudoSae.view.WizardSurvival.Entities;
using VraiPseudoSae.view.WizardSurvival.Rendering;
using VraiPseudoSae.view.WizardSurvival.Spells;

namespace VraiPseudoSae.view.WizardSurvival;

/// <summary>
/// IUTGame implementation of the Pyxel wizard survival prototype.
/// </summary>
public sealed class WizardSurvivalGame : Game
{
    public const double ViewportWidth = 800;
    public const double ViewportHeight = 600;
    private const int InitialZombieCount = 10;
    private const int StartingScore = 1000;

    private readonly List<WorldItem> worldItems = new();
    private readonly List<ZombieEnemy> zombies = new();
    private readonly List<FireballProjectile> projectiles = new();
    private readonly List<IVisualEffect> effects = new();
    private readonly IRandomSource random;
    private double spawnTimer;
    private double spawnDelay = 2;
    private double spawnChance = 0.8;
    private int lastHealScore;

    public WizardSurvivalGame(
        IScreen screen,
        string spritesFolder,
        string soundsFolder,
        ICollisionMap? map = null,
        IRandomSource? random = null)
        : base(screen, spritesFolder, soundsFolder, 60)
    {
        Map = map ?? WizardArenaMap.CreateDefault();
        this.random = random ?? new SystemRandomSource();
        Camera = new Camera2D(ViewportWidth, ViewportHeight, Map.Width, Map.Height);
        Fireball = new FireballSpell();
        CelestialCall = new CelestialCallSpell();
        Shield = new ShieldSpell();
        Laser = new LaserSpell();
        UpgradeService = new WizardUpgradeService();
    }

    public WizardGameState State { get; private set; } = WizardGameState.Menu;

    public ICollisionMap Map { get; }

    public Camera2D Camera { get; }

    public WizardInputController Controller { get; private set; } = null!;

    public WizardPlayer Player { get; private set; } = null!;

    public IReadOnlyList<ZombieEnemy> Zombies => zombies;

    public IReadOnlyList<FireballProjectile> Projectiles => projectiles;

    public IReadOnlyList<IVisualEffect> Effects => effects;

    public FireballSpell Fireball { get; private set; }

    public CelestialCallSpell CelestialCall { get; private set; }

    public ShieldSpell Shield { get; private set; }

    public LaserSpell Laser { get; private set; }

    public IUpgradeService UpgradeService { get; }

    public int Score { get; private set; }

    public int FinalScore { get; private set; }

    public int ZombiesKilled { get; private set; }

    public Action<WizardGameState>? StateChanged { get; set; }

    public Action<WizardHudSnapshot>? HudChanged { get; set; }

    public Action<Camera2D>? CameraChanged { get; set; }

    protected override void InitItems()
    {
        Controller = new WizardInputController(this);
        AddItem(Controller);
        PublishState();
        PublishHud();
    }

    public void StartNewGame()
    {
        ClearRunItems();
        Score = StartingScore;
        FinalScore = 0;
        ZombiesKilled = 0;
        spawnChance = 0.8;
        spawnTimer = 0;
        spawnDelay = 2;
        lastHealScore = Score;
        Fireball = new FireballSpell();
        CelestialCall = new CelestialCallSpell();
        Shield = new ShieldSpell();
        Laser = new LaserSpell();

        Player = new WizardPlayer(90, 90, this);
        RegisterWorldItem(Player);

        for (int i = 0; i < InitialZombieCount; i++)
            SpawnZombie();

        Camera.CenterOn(Player.CenterX, Player.CenterY);
        SyncWorldItems();
        ChangeState(WizardGameState.Playing);
        PublishHud();
    }

    public void Tick(double seconds, DoubleVector movementInput)
    {
        if (State != WizardGameState.Playing)
            return;

        Fireball.Tick(this, seconds);
        CelestialCall.Tick(this, seconds);
        Shield.Tick(this, seconds);
        Laser.Tick(this, seconds);

        if (!Laser.LocksMovement)
            Player.Move(movementInput, seconds, Map);

        Player.TickLiving(seconds);
        HealPlayerFromScore();

        foreach (FireballProjectile projectile in projectiles.ToArray())
            projectile.Tick(seconds);

        foreach (IVisualEffect effect in effects.ToArray())
            effect.Tick(this, seconds);

        effects.RemoveAll(effect => !effect.IsActive);
        RemoveInactiveProjectiles();
        RemoveDeadZombies();

        foreach (ZombieEnemy zombie in zombies.ToArray())
            zombie.Tick(Player, seconds, Map, random);

        HandleContactDamage();
        RemoveDeadZombies();
        HandleSpawning(seconds);
        UpdateCameraAndSprites();
        PublishHud();
    }

    public void AddProjectile(FireballProjectile projectile)
    {
        projectiles.Add(projectile);
        RegisterWorldItem(projectile);
    }

    public void AddEffect(IVisualEffect effect) => effects.Add(effect);

    public void CreateBurst(double worldX, double worldY, string palette) =>
        effects.Add(new ParticleEffect(worldX, worldY, palette, 16, random));

    public void AddScore(int amount)
    {
        Score = Math.Max(0, Score + amount);
        PublishHud();
    }

    public bool ApplyUpgrade(UpgradeKind upgrade) => UpgradeService.Apply(this, upgrade);

    public void OpenUpgradePanel()
    {
        if (State == WizardGameState.Playing)
            ChangeState(WizardGameState.Upgrade);
    }

    public void CloseUpgradePanel()
    {
        if (State == WizardGameState.Upgrade)
            ChangeState(WizardGameState.Playing);
    }

    public void TogglePause()
    {
        if (State == WizardGameState.Playing)
            ChangeState(WizardGameState.Paused);
        else if (State == WizardGameState.Paused)
            ChangeState(WizardGameState.Playing);
    }

    public void ReturnToMenu()
    {
        ClearRunItems();
        ChangeState(WizardGameState.Menu);
        PublishHud();
    }

    public void HandleEscape()
    {
        if (State == WizardGameState.Upgrade)
            CloseUpgradePanel();
        else if (State is WizardGameState.Playing or WizardGameState.Paused)
            TogglePause();
    }

    public void SyncWorldItems()
    {
        foreach (WorldItem item in worldItems.Where(item => item.IsActive))
            item.SyncScreenPosition();
    }

    public void SpawnZombie()
    {
        ZombieKind kind = ChooseZombieKind();
        ZombieEnemy? zombie = CreateZombieAwayFromPlayer(kind);
        if (zombie is null)
            return;

        zombies.Add(zombie);
        RegisterWorldItem(zombie);
    }

    protected override void RunWhenWin()
    {
    }

    protected override void RunWhenLoose()
    {
    }

    private void RegisterWorldItem(WorldItem item)
    {
        worldItems.Add(item);
        AddItem(item);
    }

    private ZombieKind ChooseZombieKind()
    {
        int threshold = Math.Max(5, 10 - ZombiesKilled / 10);
        int evolutionRoll = random.Next(0, 11);
        return evolutionRoll < threshold ? ZombieKind.Normal : ZombieKind.Evolved;
    }

    private ZombieEnemy? CreateZombieAwayFromPlayer(ZombieKind kind)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            double x = random.NextDouble(34, Map.Width - 68);
            double y = random.NextDouble(34, Map.Height - 68);
            var candidate = new System.Windows.Rect(x, y, 34, 38);
            if (!Map.CanOccupy(candidate))
                continue;

            double minDistance = 180 + ZombiesKilled * 2;
            if (Player is not null)
            {
                double dx = (x + 17) - Player.CenterX;
                double dy = (y + 19) - Player.CenterY;
                if (Math.Sqrt(dx * dx + dy * dy) < Math.Min(360, minDistance))
                    continue;
            }

            return new ZombieEnemy(x, y, this, kind);
        }

        return null;
    }

    private void ClearRunItems()
    {
        foreach (WorldItem item in worldItems)
            RemoveItem(item);

        worldItems.Clear();
        zombies.Clear();
        projectiles.Clear();
        effects.Clear();
    }

    private void RemoveInactiveProjectiles()
    {
        foreach (FireballProjectile projectile in projectiles.Where(projectile => !projectile.IsActive).ToArray())
        {
            projectiles.Remove(projectile);
            worldItems.Remove(projectile);
            RemoveItem(projectile);
        }
    }

    private void RemoveDeadZombies()
    {
        foreach (ZombieEnemy zombie in zombies.Where(zombie => !zombie.IsActive).ToArray())
        {
            zombies.Remove(zombie);
            worldItems.Remove(zombie);
            RemoveItem(zombie);
            Score += zombie.ScoreValue;
            ZombiesKilled++;

            if (random.NextDouble() > 0.6)
                spawnChance = Math.Max(0.1, spawnChance - zombie.SpawnChancePenalty);

            CreateBurst(zombie.CenterX, zombie.CenterY, "death");
        }
    }

    private void HandleContactDamage()
    {
        if (Shield.IsActive || Player.Health <= 0)
            return;

        foreach (ZombieEnemy zombie in zombies)
        {
            if (!zombie.IsActive || !Player.CircleIntersects(zombie))
                continue;

            int damage = zombie.Kind == ZombieKind.Normal ? 1 : 2;
            if (Player.TakeDamage(new DamageRequest(damage, zombie.Kind.ToString())))
                CreateBurst(Player.CenterX, Player.CenterY, "hurt");

            if (Player.Health <= 0)
            {
                FinalScore = Score;
                ChangeState(WizardGameState.GameOver);
            }

            return;
        }
    }

    private void HandleSpawning(double seconds)
    {
        if (ZombiesKilled < 6)
            return;

        spawnTimer -= seconds;
        if (spawnTimer > 0)
            return;

        int count = Math.Min(3, 1 + (ZombiesKilled - 6) / 5);
        int maxZombies = 10 + ZombiesKilled / 3;

        for (int i = 0; i < count; i++)
        {
            if (random.NextDouble() > spawnChance && zombies.Count < maxZombies)
                SpawnZombie();
        }

        spawnDelay = Math.Max(0.5, 2 - (ZombiesKilled - 6) * 0.1);
        spawnTimer = spawnDelay;
    }

    private void HealPlayerFromScore()
    {
        if (Score >= lastHealScore + 50 && Player.Health < Player.MaxHealth)
        {
            Player.HealOne();
            lastHealScore = Score;
        }
    }

    private void UpdateCameraAndSprites()
    {
        Camera.CenterOn(Player.CenterX, Player.CenterY);
        SyncWorldItems();
        CameraChanged?.Invoke(Camera);
    }

    private void ChangeState(WizardGameState state)
    {
        State = state;
        PublishState();
        PublishHud();
    }

    private void PublishState() => StateChanged?.Invoke(State);

    private void PublishHud()
    {
        int lives = Player?.Health ?? 0;
        int maxLives = Player?.MaxHealth ?? 4;
        HudChanged?.Invoke(new WizardHudSnapshot(
            Score,
            FinalScore,
            lives,
            maxLives,
            ZombiesKilled,
            Fireball.Cooldown.Progress,
            CelestialCall.Cooldown.Progress,
            Shield.Cooldown.Progress,
            Laser.Cooldown.Progress,
            Shield.IsActive,
            Laser.IsActive));
    }
}
