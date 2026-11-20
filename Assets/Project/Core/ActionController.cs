using System;
using System.Collections.Generic;
using System.Text;
using TMPro.EditorUtilities;

public class ActionController
{
    private readonly SensorContextService _state;

    public ActionController(SensorContextService state) => _state = state;

    public void TriggerClap(bool flag) => _state.SetClap(flag);
    public void TriggerBlow(bool flag) => _state.SetBlow(flag);
    public void TriggerLight(bool flag) => _state.SetLight(flag);

    public SensorContext GetContext() => _state.Snapshot();
}