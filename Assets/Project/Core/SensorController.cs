using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

public class SensorController
{
    private readonly SensorHandler _sensorHandler = SensorHandler.CreateHandler();


    public SensorContext EvaluateMeasurementsAndDecideAction(float peak, float rms, float brightness, float now, SensorContext pressed)
    {
        SensorContext context = new SensorContext();
        _sensorHandler.UpdateContext(peak, rms, brightness, now, context);  

        context.HasClap |= pressed.HasClap;
        context.HasBlow |= pressed.HasBlow;
        context.HasLight |= pressed.HasLight;
        return context;
    }


}