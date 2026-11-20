using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

public class SensorController
{
    private readonly SensorContextService _state;
    private readonly SensorHandler _sensorHandler = SensorHandler.CreateHandler();

    public SensorController(SensorContextService state) => _state = state;

    public SensorContext EvaluateMeasurementsAndDecideAction(float peak, float rms, float brightness, float now)
    {
        _state.Apply(ctx => _sensorHandler.UpdateContext(peak, rms, brightness, now, ctx));
        return _state.Snapshot();
    }
}