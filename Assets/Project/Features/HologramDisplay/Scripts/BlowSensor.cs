using System;
using UnityEngine;

public class BlowSensor
{
    private const float GATE = 0.17f;
    private const float BLOW_MIN_DURATION_IN_SECONDS = 0.15f;

    private float maxRms;

    // call EVERY frame
    public bool Detect(bool soundStarted, bool isLoud, bool soundEnded,
                       float rms, float durationInSeconds)
    {
        if (soundStarted)
            maxRms = 0f;

        if (isLoud)
            maxRms = Mathf.Max(maxRms, rms);

        return soundEnded
            && durationInSeconds >= BLOW_MIN_DURATION_IN_SECONDS
            && maxRms >= GATE;
    }
}