using System;
using System.Collections.Generic;
using UnityEngine;

public class ShapeSwitcher : MonoBehaviour
{
    [Tooltip("Shapes placed in the scene. Only one of them is shown at a time.")]
    public List<GameObject> shapes = new List<GameObject>();

    [Tooltip("Whether the shapes spin when the scene starts.")]
    public bool spinning = true;

    private int current = 0;

     private float heldPeak, heldRms, holdTimer, crest, crestAtPeak;
     private float gate = 0.17f;  
     private float lastDuration; 

    private ShapeSwitchUI _shapeSwitchUI;

     private SensorDebugUI _debugUI;

    private HostScreenUI _hostScreenUI;

    private ShapeReactionDispatcher _dispatcher;


    private TextContext _textContext;


    private int _selectedVariant = RemoteProtocol.NO_INDEX;

    //Only set in the web build, other platforms have no remote link.
    private RemoteLink _remoteLink;
    private RemoteLinkUI _remoteLinkUI;

    //Only set when this device joined a host as a remote. It then shows no hologram, only the remote's buttons.
    private RemoteDeviceUI _remoteDeviceUI;

    //ActionController
    private ActionController _actionController;

    private SensorController _sensorController;

    private MeasurementController _measurementController;

    private SensorContextService sensorContextService;

    private LastActionTimeService lastActionTimeService;

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
        _textContext = new TextContext("Connect and Write");
        sensorContextService = new SensorContextService();
        lastActionTimeService = new LastActionTimeService();
        _actionController = new ActionController(sensorContextService, lastActionTimeService);
        _sensorController = new SensorController();
        _measurementController = new MeasurementController();

#if UNITY_WEBGL && !UNITY_EDITOR
        WebRemoteLinkPlatform remoteLinkPlatform = new WebRemoteLinkPlatform();
        _remoteLink = new RemoteLink(remoteLinkPlatform, Application.absoluteURL, Application.streamingAssetsPath, new ShapeSwitcherControls(this, _textContext));
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
        _debugUI = new SensorDebugUI();
        _hostScreenUI = new HostScreenUI(_shapeSwitchUI, _remoteLinkUI, _debugUI);
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

    public int CurrentIndex => current;

    void OnGUI()
    {
        if (_remoteDeviceUI != null)
        {
            _remoteDeviceUI.Draw(_remoteLink, _textContext, _actionController);
            return;
        }

        if (shapes == null || shapes.Count == 0 || current >= shapes.Count || shapes[current] == null)
        {
            return;
        }

        IShapeCycler cycler = shapes[current].GetComponentInChildren<IShapeCycler>();

        MeasurementContext previousMeasurements = _measurementController.GetPreviousMeasurement();
        SensorDebugData debugData = new SensorDebugData
        {
            Peak = previousMeasurements.Peak,
            HeldPeak = heldPeak,
            Rms = previousMeasurements.Rms,
            HeldRms = heldRms,
            Crest = crest,
            CrestAtPeak = crestAtPeak,
            LastDuration = lastDuration,
            Gate = gate,
            Brightness = previousMeasurements.Brightness,
            Context = _actionController.GetContext()
        };

        _hostScreenUI.Draw(cycler, spinning, _textContext, _remoteLink, debugData);
    }

    public void RunSensor()
    {
        if ( shapes == null || current >= shapes.Count || shapes[current] == null)
        {
            Debug.Log("Runsensor failed");
            return;
        }

        //Messages from other devices that arrived since the last frame
        _remoteLink?.ProcessIncoming(Time.unscaledTime);

        //Measurements
        MeasurementContext measurements = _measurementController.Measure();

        //Sensor COntroller
        SensorContext sensorContext = _sensorController.EvaluateMeasurementsAndDecideAction(measurements.Peak, measurements.Rms, measurements.Brightness, Time.time, _actionController.GetContext());



        //Host: adds what the remotes detected. Remote: sends what was just detected to the host.
        _remoteLink?.Apply(sensorContext, _textContext, Time.unscaledTime);

        //Updates Context of the Controller
        sensorContext = _actionController.UpdateContext(sensorContext, Time.time);
        _actionController.Tick(Time.time);

        //for debugging
        holdTimer -= Time.deltaTime;
        if (holdTimer <= 0f) { heldPeak = 0f; heldRms = 0f; holdTimer = 2f; }
        if (measurements.Peak > heldPeak)          // new loudest moment in this 2 s window
            crestAtPeak = crest;
        heldPeak = Mathf.Max(heldPeak, measurements.Peak);
        heldRms  = Mathf.Max(heldRms, measurements.Rms);



        //A remote has no shapes on screen that could react, its results went to the host above.
        if (_remoteDeviceUI == null)
        {
            //The variant dropdown was drawn in OnGUI() after the last frame
            _selectedVariant = _shapeSwitchUI.ConsumeSelectedVariant();
            _dispatcher.Dispatch(sensorContext, _textContext, shapes[current], _selectedVariant);
        }

        _actionController.ResetActions(Time.time);
    }
}