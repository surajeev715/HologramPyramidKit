using UnityEngine;

public class SoundSensor
{

    private AudioClip Mic;

    private float threshold = 0.25f;
    private float cooldown = 0.3f;
    float timer = 0f;

    public SoundSensor()
    { 
        Mic = Microphone.Start(null, true, 1, 44100);
    }

    public bool Sense()
    {
        //Measure sound
        if (Mic == null) return false;
        timer -= Time.deltaTime;

        float[] data = new float[256];
        Mic.GetData(data, 0);

        float loudness = 0f;
        foreach (float f in data)
            loudness += Mathf.Abs(f);

        return loudness > threshold && timer <= 0f;
    }
}