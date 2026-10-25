using System;
using System.Collections.Generic;
using UnityEngine;

public class ShapeSwitcher : MonoBehaviour
{
    [Tooltip("Shapes placed in the scene. Only one of them is shown at a time.")]
    public List<GameObject> shapes = new List<GameObject>();

    [Tooltip("Whether the shapes spin when the scene starts.")]
    public bool spinning = true;


    [Header("Buttons")]
    public float buttonWidthCm = 3.5f;
    public float buttonHeightCm = 1f;
    public float buttonMarginCm = 0.3f;

    [Header("Sound")]
    [Tooltip("Peak level (0 to 1) the microphone has to reach to count as a loud sound.")]
    [Range(0f, 1f)]
    public float soundThreshold = 0.4f;

    [Tooltip("Seconds after a detected sound in which no new sound is detected.")]
    public float soundCooldownSeconds = 0.3f;

    private int current = 0;
    private GUIStyle buttonStyle;

    private SoundSensor _soundSensor;
    public bool _hasSoundOccured;
#if UNITY_ANDROID && !UNITY_EDITOR
     //keep value display logic for future debugging
     private LightReaderSensor _lightSensor;
     private AndroidMicrophoneInputHandler _androidMicrophone;
     private float peak, rms;
     private float heldPeak, heldRms, holdTimer, crest, crestAtPeak;
     private const float GATE = 0.17f;
     private bool isLoud = false;
     private float loudStartTime;
     private float lastDuration; 
     private BlowSensor _blowSensor;
     private ClapSensor _clapSensor;
     private bool _hasClapOccured = false;
     private bool _hasBlowOccured = false;
#endif

    public void SetShapes(List<GameObject> shapes)
    {
        if (shapes == null)
        {
            throw new ArgumentNullException(nameof(shapes), "Shape list cannot be null!");
        }
        this.shapes = shapes;
        Debug.Log($"[ShapeSwitcher] Shapes zugewiesen. Anzahl: {this.shapes.Count}");
    }

    public void StartSwitcher()
    {
        if (shapes == null || shapes.Count == 0)
        {
            Debug.LogWarning("[ShapeSwitcher] StartSwitcher aufgerufen, aber 'shapes' ist leer!");
            return;
        }
        Show(0);
        SetSpinning(spinning);
        _soundSensor = new SoundSensor(soundThreshold, soundCooldownSeconds);
#if UNITY_ANDROID && !UNITY_EDITOR
        _lightSensor = new LightReaderSensor();
        _androidMicrophone = new AndroidMicrophoneInputHandler();
        _blowSensor = new BlowSensor();
        _clapSensor = new ClapSensor();
#endif
        _hasSoundOccured = false;
    }

    public void Show(int index)
    {
        if (shapes == null || shapes.Count == 0) return;

        // Sicherstellen, dass der Index innerhalb der Grenzen bleibt
        current = Mathf.Clamp(index, 0, shapes.Count - 1);

        for (int i = 0; i < shapes.Count; i++)
        {
            if (shapes[i] != null)
            {
                bool shouldBeActive = (i == current);
                shapes[i].SetActive(shouldBeActive);
            }
        }

        Debug.Log($"[ShapeSwitcher] Zeige Form Index {current}: {shapes[current].name}");
    }

    public void NextShape()
    {
        if (shapes == null || shapes.Count == 0) return;
        int nextIndex = (current + 1) % shapes.Count;
        Show(nextIndex);
    }

    void SetSpinning(bool value)
    {
        spinning = value;

        foreach (GameObject shape in shapes)
        {
            if (shape == null) continue;

            foreach (CubeAnimation animation in shape.GetComponentsInChildren<CubeAnimation>(true))
            {
                animation.enabled = spinning;
            }
        }
    }

    void OnGUI()
    {
        if (shapes == null || shapes.Count == 0 || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        // DPI Fallback für korrekte Button-Größe im Editor
        float dpi = Screen.dpi > 0 ? Screen.dpi : 96f;
        float pixelsPerCm = dpi / 2.54f;

        float width = buttonWidthCm * pixelsPerCm;
        float height = buttonHeightCm * pixelsPerCm;
        float margin = buttonMarginCm * pixelsPerCm;

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleCenter;
        }
        buttonStyle.fontSize = Mathf.RoundToInt(height * 0.35f);

        Rect shapeRect = new Rect(margin, Screen.height - height - margin, width, height);
        Rect spinRect = new Rect(Screen.width - width - margin, Screen.height - height - margin, width, height);

#if UNITY_ANDROID && !UNITY_EDITOR
        //keep value display logic for future debugging
        GUI.skin.label.fontSize = 50;
        GUILayout.Label($"Peak: {peak:F3}   (max 2s: {heldPeak:F3})");
        GUILayout.Label($"RMS:  {rms:F3}   (max 2s: {heldRms:F3})");
        GUILayout.Label($"Crest: {crest:F1}   (at max peak: {crestAtPeak:F1})");
        GUILayout.Label($"Last duration: {lastDuration:F3} s");
        GUILayout.Label($"is Clap: {_hasClapOccured}");
        GUILayout.Label($"is Blow: {_hasBlowOccured}");
#endif
        // Shape Button (Klick führt NextShape aus)
        if (GUI.Button(shapeRect, shapes[current].name, buttonStyle))
        {
            NextShape();
        }

        // Spin Button
        if (GUI.Button(spinRect, spinning ? "Spin: On" : "Spin: Off", buttonStyle))
        {
            SetSpinning(!spinning);
        }

        // Animation Button, nur für Formen mit mehreren Animationen (z.B. die Katze), über dem Shape Button
        AnimationCycler cycler = shapes[current].GetComponentInChildren<AnimationCycler>();

        if (cycler != null && cycler.states.Length > 1)
        {
            Rect animationRect = new Rect(margin, shapeRect.y - height - margin, width, height);

            if (GUI.Button(animationRect, cycler.CurrentState, buttonStyle))
            {
                cycler.Next();
            }
        }
    }

    public void RunSensor()
    {
        if (_soundSensor == null || shapes == null || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        _hasSoundOccured = _soundSensor.Sense();

        bool hasLightTrigger = false;
#if UNITY_ANDROID && !UNITY_EDITOR
        hasLightTrigger = _lightSensor.Detect();
        peak = _androidMicrophone.MeasureLoudness();
        rms = _androidMicrophone.LastRms;
        crest = peak / Mathf.Max(rms, 0.0001f);

        //keep value display logic for future debugging
        bool gateOpen = rms > GATE;
        bool soundStarted = false;
        bool soundEnded = false;

        if (gateOpen && !isLoud)
            {
                loudStartTime = Time.time;
                isLoud = true;
                soundStarted = true;
            }
        else if (!gateOpen && isLoud)
            {
                lastDuration = Time.time - loudStartTime;
                isLoud = false;
                soundEnded = true;
            }

        // sensors decide, every frame
        _hasClapOccured = _clapSensor.Detect(soundStarted, isLoud, soundEnded, peak, rms, lastDuration);
        _hasBlowOccured = _blowSensor.Detect(soundStarted, isLoud, soundEnded, rms, lastDuration);

        holdTimer -= Time.deltaTime;
        if (holdTimer <= 0f) { heldPeak = 0f; heldRms = 0f; holdTimer = 2f; }
        if (peak > heldPeak)          // new loudest moment in this 2 s window
            crestAtPeak = crest;
        heldPeak = Mathf.Max(heldPeak, peak);
        heldRms  = Mathf.Max(heldRms, rms);
#endif


        //Only the balloon reacts to sounds, it pops.
        BalloonPopper balloon = shapes[current].GetComponentInChildren<BalloonPopper>();

        if (_hasSoundOccured && (balloon != null))
        {
            balloon.Pop();
        }

        //Only Sensortest

        SensorTestScript sensorTest = shapes[current].GetComponentInChildren<SensorTestScript>();

        if ((sensorTest != null))
        {

            if (_hasSoundOccured)
            {
                sensorTest.switchBetween();
            }

            else if (hasLightTrigger)
            {
                sensorTest.setColorToYellow();
            }
        }

        //Only Candle Reacts to input

#if UNITY_ANDROID && !UNITY_EDITOR
    CandleAnimationScript animationScript = shapes[current].GetComponentInChildren<CandleAnimationScript>();

        if ((animationScript != null))
        {
            animationScript.Animate(hasLightTrigger,_hasBlowOccured);
        }
        else
        {
            Debug.LogWarning("[CANDLE] No CandleAnimationScript found on " + shapes[current].name);
        }
#endif
    }
}