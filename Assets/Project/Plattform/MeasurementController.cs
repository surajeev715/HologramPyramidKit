using System;
using System.Collections.Generic;
using System.Text;

public class MeasurementController
{
    private readonly IMicrophoneInputHandler _microphoneInputHandler;
    private readonly WebLightInputHandler _webLightInput;

    private MeasurementContext _previousMeasurement;

    public MeasurementController()
    {
        _webLightInput = new WebLightInputHandler();

#if UNITY_EDITOR
        _microphoneInputHandler = new EditorMicrophoneInputHandler();
#elif UNITY_WEBGL
        _microphoneInputHandler = new WebMicrophoneInputHandler();
#else
        throw new System.PlatformNotSupportedException();
#endif
        _previousMeasurement = new MeasurementContext(0, 0, 0);
    }

    public MeasurementContext Measure()
    {
        float peak = _microphoneInputHandler.MeasureLoudness();
        float rms = _microphoneInputHandler.GetLastRms();
        float brightness = _webLightInput.MeasureBrightness();

        MeasurementContext newMeasurement = new MeasurementContext(rms, peak, brightness);

        _previousMeasurement = newMeasurement;
        return newMeasurement;
    }

    public MeasurementContext GetPreviousMeasurement()
    {
        return _previousMeasurement;
    }
}