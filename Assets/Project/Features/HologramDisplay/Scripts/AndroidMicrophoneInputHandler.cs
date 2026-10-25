using System;
using UnityEngine;
using UnityEngine.Android;

public class AndroidMicrophoneInputHandler
{
    private const int SAMPLE_RATE_IN_FREQUENCY_HZ = 44100;
    private const int CLIP_DURATION_IN_SECONDS = 1;

    //About 23 ms of audio, enough to cover the time between two frames.
    private const int sampleWindow = 1024;

    private AudioClip Mic;
    private float[] data = new float[sampleWindow];

    private bool micStartedSuccessfully = false;

    public float LastRms { get; private set; }

    public AndroidMicrophoneInputHandler()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
    }

    private int AdjustSampleRateToDeviceCapacity(int sampleRate, int minFrequency, int maxFrequency)
    {
        return (minFrequency == 0 && maxFrequency == 0) ? sampleRate : Mathf.Clamp(sampleRate, minFrequency, maxFrequency);
    }

    private void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[AndroidMicrophoneInputHandler] No microphone found, sounds are not detected.");
            return;
        }

        //Some devices, especially on Android, only record at certain sample rates. 0 for both means any rate works.
        Microphone.GetDeviceCaps(null, out int minFrequency, out int maxFrequency);
        //int frequency = (minFrequency == 0 && maxFrequency == 0) ? sampleRate : Mathf.Clamp(sampleRate, minFrequency, maxFrequency);
        int adjustedFrequency = AdjustSampleRateToDeviceCapacity(SAMPLE_RATE_IN_FREQUENCY_HZ, minFrequency, maxFrequency);

        //deviceName := (null) uses the default mic of the device, loop := when true as soon a clip full, overwrite/record from beginning, lengthSec := Clip duration, frequency := SampleRate
        Mic = Microphone.Start(null, true, CLIP_DURATION_IN_SECONDS, adjustedFrequency);

        micStartedSuccessfully = (Mic != null);
        if (!micStartedSuccessfully)
        {
            Debug.LogWarning("[AndroidMicrophoneInputHandler] No microphone has been started");
        }
        

    }

    public float MeasureLoudness()
    {
        if (!micStartedSuccessfully && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            StartMicrophone();
        }


        if (Mic == null) return 0f;

        //Read the newest samples, the ones right before the current recording position. GetData wraps around the end of the looping clip.
        int position = Microphone.GetPosition(null) - sampleWindow;

        if (position < 0)
        {
            position += Mic.samples;
        }

        Mic.GetData(data, position);

        float loudness = 0f;
        float sumOfSquares = 0f;
        foreach (float f in data)
        {
            loudness = Mathf.Max(loudness, Mathf.Abs(f));
            sumOfSquares += f * f;
        }
        LastRms = Mathf.Sqrt(sumOfSquares / data.Length);
        return loudness;
    }
}
