using UnityEngine;

public class SensorContext
{
    public bool HasClap { get; set; }
    public bool HasBlow { get; set; }
    public bool HasLight { get; set; }

    public bool IsCalibrating { get; set; }
    // later: brightness, loudness, whatever new inputs you add

    public SensorContext(bool hasClap, bool hasBlow, bool hasLight, bool isCalibrating)
    {
        HasClap = hasClap;
        HasBlow = hasBlow;
        HasLight = hasLight;
        IsCalibrating = isCalibrating;
    }

    public SensorContext() : this(false, false, false, false)
    {
    }

    public void SetToCalibrationContext()
    {
        HasClap = false;
        HasBlow = false;
        HasLight = false;
        IsCalibrating = true;
    }

    public void SetToNonCalibrationContext()
    {
        IsCalibrating = false;
    }

    public SensorContext Copy()
    {
        return new SensorContext(HasClap, HasBlow, HasLight, IsCalibrating);
    }
}
