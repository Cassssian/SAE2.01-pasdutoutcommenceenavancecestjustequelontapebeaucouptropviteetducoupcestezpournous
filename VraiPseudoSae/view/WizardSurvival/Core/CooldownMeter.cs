namespace VraiPseudoSae.view.WizardSurvival.Core;

/// <summary>
/// Tracks a reusable ability cooldown in seconds.
/// </summary>
public sealed class CooldownMeter
{
    public CooldownMeter(double duration)
    {
        Duration = duration;
    }

    public double Duration { get; private set; }

    public double Remaining { get; private set; }

    public bool IsReady => Remaining <= 0;

    public double Progress => Duration <= 0 ? 1 : 1 - Remaining / Duration;

    public void Restart() => Remaining = Duration;

    public void Tick(double seconds)
    {
        if (Remaining > 0)
            Remaining = System.Math.Max(0, Remaining - seconds);
    }

    public void SetDuration(double duration)
    {
        Duration = System.Math.Max(0.05, duration);
        Remaining = System.Math.Min(Remaining, Duration);
    }

    public void Reset() => Remaining = 0;
}
