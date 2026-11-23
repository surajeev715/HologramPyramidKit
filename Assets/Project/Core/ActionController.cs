using System;

public class ActionController
{
    private readonly SensorContextService _state;
    private readonly LastActionTimeService _lastActions;

    public ActionController(SensorContextService state, LastActionTimeService lastActions)
    {
        _state = state;
        _lastActions = lastActions;
    }

    public void TriggerClap(bool flag, float now)
    {
        _state.SetClap(flag);
        if (flag) _lastActions.RegisterClap(now);
    }

    public void TriggerBlow(bool flag, float now)
    {
        _state.SetBlow(flag);
        if (flag) _lastActions.RegisterBlow(now);
    }

    public void TriggerLight(bool flag, float now)
    {
        _state.SetLight(flag);
        if (flag) _lastActions.RegisterLight(now);
    }

    public bool IsClapActive() => _lastActions.IsClapActive();

    public bool IsBlowActive() => _lastActions.IsBlowActive();

    public bool IsLightActive() => _lastActions.IsLightActive();
    // call once per frame
    public void Tick(float now) => _lastActions.Update(now);

    public SensorContext GetContext() => _state.Snapshot();

    public SensorContext UpdateContext(SensorContext newContext, float now)
    {
        SensorContext current = _state.Snapshot();

        bool hasClap = current.HasClap || newContext.HasClap;
        bool hasBlow = current.HasBlow || newContext.HasBlow;
        bool hasLight = current.HasLight || newContext.HasLight;

        TriggerClap(hasClap, now);
        TriggerBlow(hasBlow, now);
        TriggerLight(hasLight, now);

        return _state.Snapshot();
    }

    public void ResetActions(float now)
    {
        TriggerClap(false, now);
        TriggerBlow(false, now);
        TriggerLight(false, now);
    }

}