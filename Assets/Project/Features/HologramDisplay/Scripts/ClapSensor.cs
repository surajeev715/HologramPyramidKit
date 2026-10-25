using System;
using UnityEngine;

public class ClapSensor
{
    private const float GATE = 0.17f;
    private const float CLAP_MAX_DURATION_IN_SECONDS = 0.07f;
    private const float CLAP_MIN_PEAK = 0.5f;

    private float maxPeak, maxRms;

    // call EVERY frame
    public bool Detect(bool soundStarted, bool isLoud, bool soundEnded,
                       float peak, float rms, float durationInSeconds)
    {
        if (soundStarted)
        {
            maxPeak = 0f;
            maxRms = 0f;
        }

        if (isLoud)
        {
            maxPeak = Mathf.Max(maxPeak, peak);
            maxRms = Mathf.Max(maxRms, rms);
        }

        return soundEnded
            && durationInSeconds <= CLAP_MAX_DURATION_IN_SECONDS
            && maxPeak >= CLAP_MIN_PEAK
            && maxRms >= GATE;
    }
}