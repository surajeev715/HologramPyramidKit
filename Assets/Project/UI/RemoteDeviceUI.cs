using UnityEngine;

//The whole screen of a remote: the state of the connection, the buttons that control the host's hologram,
//and one field per sensor that lights up when this device detects it and triggers it by hand when tapped.
public class RemoteDeviceUI
{
    private const float MAX_WIDTH_CM = 8f;
    private const float BUTTON_HEIGHT_CM = 1.3f;
    private const float MARGIN_CM = 0.4f;
    private const float BUTTON_FONT_SCALE = 0.32f;
    private const float LABEL_FONT_SCALE = 0.26f;

    //A clap is detected in a single frame, so its field stays lit long enough to be seen.
    private const float INDICATOR_HOLD_IN_SECONDS = 0.4f;

    private static readonly Color INDICATOR_ON = new Color(1f, 0.85f, 0.2f);

    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;

    public void Draw(RemoteLink link)
    {
        RemoteDeviceSession device = link.Device;
        float pixelsPerCm = ScreenDpi.PixelsPerCm;
        float margin = MARGIN_CM * pixelsPerCm;
        float buttonHeight = BUTTON_HEIGHT_CM * pixelsPerCm;
        float width = Mathf.Min(MAX_WIDTH_CM * pixelsPerCm, Screen.width - 2f * margin);

        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.alignment = TextAnchor.MiddleCenter;

            //Names and messages come from other devices, so they are never read as rich text.
            _buttonStyle.richText = false;
            _labelStyle = new GUIStyle(GUI.skin.box);
            _labelStyle.alignment = TextAnchor.MiddleCenter;
            _labelStyle.wordWrap = true;
            _labelStyle.richText = false;
        }
        _buttonStyle.fontSize = Mathf.RoundToInt(buttonHeight * BUTTON_FONT_SCALE);
        _labelStyle.fontSize = Mathf.RoundToInt(buttonHeight * LABEL_FONT_SCALE);

        GUILayoutOption height = GUILayout.Height(buttonHeight);

        GUILayout.BeginArea(new Rect((Screen.width - width) / 2f, margin, width, Screen.height - 2f * margin));

        GUILayout.Label(link.LoadError ?? StatusText(device), _labelStyle);
        if (!string.IsNullOrEmpty(device.Hint))
        {
            GUILayout.Label(device.Hint, _labelStyle);
        }

        if (device.CanReconnect && GUILayout.Button("Reconnect", _buttonStyle, height))
        {
            device.Reconnect();
        }

        if (device.State == RemoteDeviceSession.DeviceState.Connected)
        {
            GUILayout.Space(margin);
            DrawControls(device, height);
            GUILayout.Space(margin);
            DrawSensors(device, height);
        }

        GUILayout.EndArea();
    }

    //Same buttons as on the host (ShapeSwitchUI), they show what the host shows.
    private void DrawControls(RemoteDeviceSession device, GUILayoutOption height)
    {
        GUILayout.Label("Hologram", _labelStyle);

        string shapeText = device.HostShapeName.Length > 0 ? device.HostShapeName : "Next shape";
        if (GUILayout.Button(shapeText, _buttonStyle, height))
        {
            device.RequestNextShape();
        }

        if (GUILayout.Button(device.IsHostSpinning ? "Spin: On" : "Spin: Off", _buttonStyle, height))
        {
            device.RequestSetSpinning(!device.IsHostSpinning);
        }

        //Only shapes with several animations have this button, like on the host.
        //It is disabled instead of removed, so the buttons below do not jump when the shape changes.
        bool hasAnimations = device.HostAnimationName.Length > 0;
        GUI.enabled = hasAnimations;
        if (GUILayout.Button(hasAnimations ? device.HostAnimationName : "No animations", _buttonStyle, height))
        {
            device.RequestNextAnimation();
        }
        GUI.enabled = true;
    }

    private void DrawSensors(RemoteDeviceSession device, GUILayoutOption height)
    {
        GUILayout.Label("Sensors of this device (lit when detected, tap to trigger)", _labelStyle);

        float now = Time.unscaledTime;
        GUILayout.BeginHorizontal();
        if (SensorButton("Clap", device.LastClapTime, now, height)) device.TriggerClap();
        if (SensorButton("Blow", device.LastBlowTime, now, height)) device.TriggerBlow();
        if (SensorButton("Light", device.LastLightTime, now, height)) device.TriggerLight();
        GUILayout.EndHorizontal();
    }

    private bool SensorButton(string text, float lastTime, float now, GUILayoutOption height)
    {
        Color previous = GUI.backgroundColor;
        if (now - lastTime < INDICATOR_HOLD_IN_SECONDS)
        {
            GUI.backgroundColor = INDICATOR_ON;
        }
        bool isPressed = GUILayout.Button(text, _buttonStyle, height);
        GUI.backgroundColor = previous;
        return isPressed;
    }

    private static string StatusText(RemoteDeviceSession device)
    {
        bool hasRtt = device.State == RemoteDeviceSession.DeviceState.Connected && device.RttInMilliseconds >= 0f;
        return hasRtt ? device.Status + "  (" + Mathf.RoundToInt(device.RttInMilliseconds) + " ms)" : device.Status;
    }
}
