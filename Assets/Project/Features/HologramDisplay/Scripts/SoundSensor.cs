using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

//Detects loud sounds with the microphone. On the Web the level comes from WebMicrophone.jslib, everywhere else from Unity's Microphone.
public class SoundSensor
{
    private const int sampleRate = 44100;

    //About 23 ms of audio, enough to cover the time between two frames.
    private const int sampleWindow = 1024;

    private float threshold;
    private float cooldown;
    float timer = 0f;

#if !UNITY_WEBGL || UNITY_EDITOR
    private AudioClip Mic;
    private float[] data = new float[sampleWindow];
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool micStartTried = false;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebMicrophone_Start();

    [DllImport("__Internal")]
    private static extern float WebMicrophone_GetLoudness();
#endif

    //threshold is the peak level (0 to 1) a sound has to reach, cooldown the seconds after a detection in which no new sound is detected.
    public SoundSensor(float threshold, float cooldown)
    {
        this.threshold = threshold;
        this.cooldown = cooldown;

#if UNITY_WEBGL && !UNITY_EDITOR
        //The browser asks the user for microphone access.
        WebMicrophone_Start();
#elif UNITY_ANDROID && !UNITY_EDITOR
        //Android asks the user for microphone access. The microphone is started in Sense() once it is granted.
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#else
        StartMicrophone();
#endif
    }

    public bool Sense()
    {
        //Measure sound
        timer -= Time.deltaTime;

        float loudness = MeasureLoudness();

        if (loudness < threshold || timer > 0f)
        {
            return false;
        }

        timer = cooldown;
        return true;
    }

    float MeasureLoudness()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return WebMicrophone_GetLoudness();
#else
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!micStartTried && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micStartTried = true;
            StartMicrophone();
        }
#endif
        if (Mic == null) return 0f;

        //Read the newest samples, the ones right before the current recording position. GetData wraps around the end of the looping clip.
        int position = Microphone.GetPosition(null) - sampleWindow;

        if (position < 0)
        {
            position += Mic.samples;
        }

        Mic.GetData(data, position);

        float loudness = 0f;
        foreach (float f in data)
            loudness = Mathf.Max(loudness, Mathf.Abs(f));

        return loudness;
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    void StartMicrophone()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("[SoundSensor] No microphone found, sounds are not detected.");
            return;
        }

        //Some devices, especially on Android, only record at certain sample rates. 0 for both means any rate works.
        Microphone.GetDeviceCaps(null, out int minFrequency, out int maxFrequency);
        int frequency = (minFrequency == 0 && maxFrequency == 0) ? sampleRate : Mathf.Clamp(sampleRate, minFrequency, maxFrequency);

        Mic = Microphone.Start(null, true, 1, frequency);
    }
#endif
}
