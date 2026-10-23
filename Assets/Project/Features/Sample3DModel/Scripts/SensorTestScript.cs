using UnityEngine;

public class SensorTestScript : MonoBehaviour
{
    private bool _isSoundInputAccepted;

    private AudioClip Mic;

    private float threshold = 0.25f;
    private float cooldown = 0.3f;
    float timer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _isSoundInputAccepted = false;
        Mic = Microphone.Start(null, true, 1, 44100);
    }

    // Update is called once per frame
    void Update()
     {
       //Measure sound
       if(Mic == null) return;
        timer -= Time.deltaTime;

        float[] data = new float[256];
        Mic.GetData(data, 0);

        float loudness = 0f;
        foreach (float f in data)
            loudness += Mathf.Abs(f);

        _isSoundInputAccepted = loudness > threshold && timer <= 0f;

        if (_isSoundInputAccepted) {
            //change color of prefab to yellow
            timer = cooldown;
            var renderer = GetComponent<Renderer>();
            renderer.material.color = Color.red;
        }
     }

}
