using System;
using System.Collections.Generic;
using UnityEngine;

public class ShapeSwitcher : MonoBehaviour
{
    [Tooltip("Shapes placed in the scene. Only one of them is shown at a time.")]
    public List<GameObject> shapes = new List<GameObject>();

    [Tooltip("Whether the shapes spin when the scene starts.")]
    public bool spinning = true;


    [Header("Sound")]
    [Tooltip("Peak level (0 to 1) the microphone has to reach to count as a loud sound.")]
    [Range(0f, 1f)]
    public float soundThreshold = 0.4f;

    [Tooltip("Seconds after a detected sound in which no new sound is detected.")]
    public float soundCooldownSeconds = 0.3f;

    private int current = 0;

    public bool _hasSoundOccured;

     //keep value display logic for future debugging
     private LightDomainSensor _lightSensor;
     private IMicrophoneInputHandler _microphoneInputHandler;
     private float peak, rms;
     private float heldPeak, heldRms, holdTimer, crest, crestAtPeak;
     private float gate = 0.17f;  
     private bool isLoud = false;
     private float lastDuration; 
     private ClapSensor _clapSensor;

    private ShapeSwitchUI _shapeSwitchUI;

     private SensorDebugUI _debugUI;

    private ShapeReactionDispatcher _dispatcher;

    private SensorContext _sensorContext;
     private bool _hasClapOccured = false;
     private bool _hasBlowOccured = false;

    private SensorHandler _sensorHandler;

    private bool _cycleNextPressed = false;

    //Only set in the web build, other platforms have no remote link.
    private RemoteLink _remoteLink;
    private RemoteLinkUI _remoteLinkUI;

    //Only set when this device joined a host as a remote. It then shows no hologram, only the remote's buttons.
    private RemoteDeviceUI _remoteDeviceUI;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidLightInputHandler _androidLightInput;
    private float luxMeasured;
#endif

    private WebLightInputHandler _webLightInput;
    private float brightness;


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
        _shapeSwitchUI = new ShapeSwitchUI(this);
        _dispatcher = new ShapeReactionDispatcher();
        _sensorContext = new SensorContext();
        _sensorHandler = SensorHandler.CreateHandler();
#if UNITY_ANDROID && !UNITY_EDITOR
        _microphoneInputHandler = new AndroidMicrophoneInputHandler();
        _androidLightInput = new AndroidLightInputHandler();
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        _webLightInput = new WebLightInputHandler();
        _microphoneInputHandler = new WebMicrophoneInputHandler();
        WebRemoteLinkPlatform remoteLinkPlatform = new WebRemoteLinkPlatform();
        _remoteLink = new RemoteLink(remoteLinkPlatform, Application.absoluteURL, Application.streamingAssetsPath, new ShapeSwitcherControls(this));
        if (_remoteLink.IsRemote)
        {
            _remoteDeviceUI = new RemoteDeviceUI();
            RemoteScreen.HideHologram(shapes);
        }
        else
        {
            _remoteLinkUI = new RemoteLinkUI(remoteLinkPlatform);
        }
#endif

#if UNITY_EDITOR
        _webLightInput = new WebLightInputHandler();
        _microphoneInputHandler = new EditorMicrophoneInputHandler();
#endif
        _debugUI = new SensorDebugUI();
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

    public void SetSpinning(bool value)
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

    public GameObject GetCurrentShape()
    {
        return (shapes == null || current >= shapes.Count) ? null : shapes[current];
    }

    public string getCurrentShapeName()
    {
        return shapes[current].name;
    }

    void OnGUI()
    {
        if (_remoteDeviceUI != null)
        {
            _remoteDeviceUI.Draw(_remoteLink);
            return;
        }

        if (shapes == null || shapes.Count == 0 || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        // Animation Button, nur für Formen mit mehreren Animationen (z.B. die Katze), über dem Shape Button
        AnimationCycler cycler = shapes[current].GetComponentInChildren<AnimationCycler>();

        _debugUI.Draw(peak, heldPeak, rms, heldRms, crest, crestAtPeak, lastDuration, _sensorContext, gate, brightness);

#if UNITY_ANDROID && !UNITY_EDITOR
    GUILayout.Label($"Lux: {luxMeasured}");
#endif

        _shapeSwitchUI.Draw(cycler, spinning);

        _remoteLinkUI?.Draw(_remoteLink);
    }

    public void RunSensor()
    {
        if (_sensorHandler == null || shapes == null || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        //Messages from other devices that arrived since the last frame
        _remoteLink?.ProcessIncoming(Time.unscaledTime);

        //Measurements
        peak =_microphoneInputHandler.MeasureLoudness();
        rms = _microphoneInputHandler.GetLastRms();
        crest = peak / Mathf.Max(rms, 0.0001f);

#if UNITY_ANDROID && !UNITY_EDITOR
        luxMeasured = _androidLightInput.MeasureLux();
#endif

#if UNITY_WEBGL || UNITY_EDITOR
        brightness = _webLightInput.MeasureBrightness();
#endif

        _sensorHandler.UpdateContext(peak, rms, brightness, Time.time, _sensorContext);

        //Host: adds what the remotes detected. Remote: sends what was just detected to the host.
        _remoteLink?.Apply(_sensorContext, Time.unscaledTime);

        //for debugging
        holdTimer -= Time.deltaTime;
        if (holdTimer <= 0f) { heldPeak = 0f; heldRms = 0f; holdTimer = 2f; }
        if (peak > heldPeak)          // new loudest moment in this 2 s window
            crestAtPeak = crest;
        heldPeak = Mathf.Max(heldPeak, peak);
        heldRms  = Mathf.Max(heldRms, rms);



        //A remote has no shapes on screen that could react, its results went to the host above.
        if (_remoteDeviceUI == null)
        {
            _dispatcher.Dispatch(_sensorContext, shapes[current], _cycleNextPressed);
        }
    }
}