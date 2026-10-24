using UnityEngine;
using UnityEngine.InputSystem;

public class LightReaderSensor
{
    private float _luxThreshold = 50;

    public LightReaderSensor()
    {
        Debug.Log("[LIGHT] Light Sensor initialized");
        TryEnable();
    }

    private bool TryEnable()
    {
        var sensor = LightSensor.current;
        if (sensor == null)
            return false;

        if (!sensor.enabled)
        {
            InputSystem.EnableDevice(sensor);
            sensor.samplingFrequency = 16;
            Debug.Log("[LIGHT] Light Sensor enabled: " + sensor.enabled);
        }
        return true;
    }

    public bool Detect()
    {
        if (!TryEnable())
        {
            Debug.Log("[LIGHT] No LightSensor found");
            return false;
        }

        float lux = LightSensor.current.lightLevel.ReadValue();
        Debug.Log("[LIGHT] lux = " + lux);
        return lux >= _luxThreshold;
    }
}