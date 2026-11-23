using System;
using System.Collections.Generic;
using System.Text;

public sealed class MeasurementContext
{
    public float Rms { get; }
    public float Peak { get; }
    public float Brightness { get; }

    public MeasurementContext(float rms, float peak, float brightness)
    {
        Rms = rms;
        Peak = peak;
        Brightness = brightness;
    }

    public override string ToString() =>
        $"Rms={Rms:F3} Peak={Peak:F3} Brightness={Brightness:F3}";
}