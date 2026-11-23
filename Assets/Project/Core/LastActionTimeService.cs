public class LastActionTimeService
{
    public const float ACTIVE_DURATION = 0.4f;

    private readonly Countdown _clap = new Countdown(ACTIVE_DURATION);
    private readonly Countdown _blow = new Countdown(ACTIVE_DURATION);
    private readonly Countdown _light = new Countdown(ACTIVE_DURATION);

    public float LastClapTime { get; private set; } = float.NegativeInfinity;
    public float LastBlowTime { get; private set; } = float.NegativeInfinity;
    public float LastLightTime { get; private set; } = float.NegativeInfinity;

    public void RegisterClap(float now)
    {
        LastClapTime = now;
        _clap.Start(now);
    }

    public void RegisterBlow(float now)
    {
        LastBlowTime = now;
        _blow.Start(now);
    }

    public void RegisterLight(float now)
    {
        LastLightTime = now;
        _light.Start(now);
    }

    // call once per frame
    public void Update(float now)
    {
        RunIfActive(_clap, now);
        RunIfActive(_blow, now);
        RunIfActive(_light, now);
    }

    public bool IsClapActive() => _clap.IsRunning();
    public bool IsBlowActive() => _blow.IsRunning();
    public bool IsLightActive() => _light.IsRunning();

    private static void RunIfActive(Countdown countdown, float now)
    {
        if (countdown.IsRunning())
        {
            countdown.Run(now);
        }
    }
}