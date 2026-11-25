using System.Collections.Generic;
using UnityEngine;

//The whole screen of a remote: the state of the connection, the controls of the host's hologram, and only the
//buttons the shown object can use (its sensors, the text field). Everything is sized so it fits on the screen.
public class RemoteDeviceUI
{
    private const float MAX_WIDTH_CM = 13f;
    private const float ROW_HEIGHT_CM = 1.6f;
    private const float MIN_ROW_HEIGHT_CM = 0.6f;
    private const float MARGIN_CM = 0.4f;

    private const float HEIGHT_SLACK = 1.04f;

    private const float FADE_IN_SECONDS = 0.35f;
    private const float PULSES_PER_SECOND = 1.2f;
    private const float PULSE_MIN_ALPHA = 0.35f;

    private const float HEADER = 1f + UITheme.SECTION_LABEL + UITheme.GAP;
    private const float CARD = 2f * UITheme.PADDING + UITheme.SECTION_LABEL;
    private const float SENSORS_SECTION = UITheme.GAP + CARD + 1f;
    private const float TEXT_SECTION = UITheme.GAP + CARD + 1f + UITheme.GAP + 1f;

    private const float ROWS_ABOVE_LIST = HEADER + UITheme.PADDING + UITheme.SECTION_LABEL + 1f;

    private const int UI_DEPTH = -1;

    private const string TITLE = "Hologram Remote";
    private const string WAITING_FOR_HOST = "Waiting for the host...";

    private readonly UITheme _theme = new UITheme();
    private readonly ObjectControlsUI _controls = new ObjectControlsUI();
    private readonly TextInputUI _textInput = new TextInputUI();
    private readonly ActionButtonsUI _actionButtonsUI = new ActionButtonsUI();

    private float _connectedAmount;

    public void Draw(RemoteLink link, TextContext textContext, ActionController controller)
    {
        GUI.depth = UI_DEPTH;
        GUI.color = Color.white;

        RemoteDeviceSession device = link.Device;
        bool isConnected = link.LoadError == null && device.State == RemoteDeviceSession.DeviceState.Connected;
        if (!isConnected)
        {
            _controls.CloseLists();
        }

        IReadOnlyList<string> shapeNames = device.HostShapeNames;
        IReadOnlyList<string> variantNames = device.HostVariantNames;
        bool isPicking = isConnected && _controls.IsPicking;
        ShapeFeatures features = device.HostFeatures;
        bool hasSensors = ActionButtonsUI.HasButtons(features);
        bool hasText = (features & ShapeFeatures.Text) != 0;

        float pixelsPerCm = ScreenDpi.PixelsPerCm;
        float margin = MARGIN_CM * pixelsPerCm;
        Rect safe = SafeArea();
        float height = safe.height - 2f * margin;

        float row = ROW_HEIGHT_CM * pixelsPerCm;
        if (isConnected)
        {
            float rows = ConnectedRows(hasSensors, hasText) * HEIGHT_SLACK;
            row = Mathf.Clamp(height / rows, MIN_ROW_HEIGHT_CM * pixelsPerCm, row);
        }
        _theme.Prepare(row);

        float listSpace = height - (ROWS_ABOVE_LIST + UITheme.PADDING) * row * HEIGHT_SLACK;
        _theme.DrawBackground();

        float width = Mathf.Min(MAX_WIDTH_CM * pixelsPerCm, safe.width - 2f * margin);
        GUILayout.BeginArea(new Rect(safe.x + (safe.width - width) / 2f, safe.y + margin, width, height));
        DrawHeader(link, device);
        _theme.Divider();

        _connectedAmount = isConnected ? _theme.Animate(_connectedAmount, 1f, FADE_IN_SECONDS) : 0f;
        if (isConnected)
        {
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, UITheme.Ease(_connectedAmount));

            DrawHologramCard(device, shapeNames, variantNames, listSpace);

            if (!isPicking && hasSensors)
            {
                GUILayout.Space(_theme.Gap);
                GUILayout.BeginVertical(_theme.Card);
                _theme.SectionLabel("Sensors", "tap to trigger");
                _actionButtonsUI.Draw(_theme, controller, features);
                GUILayout.EndVertical();
            }
            if (!isPicking && hasText)
            {
                GUILayout.Space(_theme.Gap);
                GUILayout.BeginVertical(_theme.Card);
                _theme.SectionLabel("Text");
                _textInput.Draw(_theme);
                GUILayout.Space(_theme.Gap);
                bool wordPerLine = _theme.SwitchRow(ObjectControlsUI.WORD_PER_LINE, device.IsHostWordPerLine);
                if (wordPerLine != device.IsHostWordPerLine)
                {
                    device.RequestSetWordPerLine(wordPerLine);
                }
                GUILayout.EndVertical();
            }
            textContext.Text = _textInput.text;
            GUI.color = previous;
        }
        else
        {
            DrawConnectionCard(link, device);
        }

        GUILayout.EndArea();
    }

    private void DrawHeader(RemoteLink link, RemoteDeviceSession device)
    {
        GUILayout.Label(TITLE, _theme.Title, GUILayout.Height(_theme.Row));

        bool hasError = link.LoadError != null;
        string status = hasError ? "Error" : device.Status;
        Color color = hasError ? UITheme.BAD : StatusColor(device.State);
        bool isBusy = !hasError && device.State != RemoteDeviceSession.DeviceState.Connected
            && device.State != RemoteDeviceSession.DeviceState.Failed;
        if (isBusy)
        {
            float wave = 0.5f + 0.5f * Mathf.Cos(Time.unscaledTime * PULSES_PER_SECOND * 2f * Mathf.PI);
            color.a = Mathf.Lerp(PULSE_MIN_ALPHA, 1f, wave);
        }
        bool hasRtt = !hasError && device.State == RemoteDeviceSession.DeviceState.Connected && device.RttInMilliseconds >= 0f;
        string rtt = hasRtt ? Mathf.RoundToInt(device.RttInMilliseconds) + " ms" : "";

        float height = _theme.SectionLabelHeight;
        GUILayout.BeginHorizontal(GUILayout.Height(height));
        GUILayout.BeginVertical();
        _theme.StatusLine(status, color, _theme.Caption, height);
        GUILayout.EndVertical();
        GUILayout.Label(rtt, _theme.CaptionRight, GUILayout.Height(height));
        GUILayout.EndHorizontal();
    }

    private void DrawHologramCard(RemoteDeviceSession device, IReadOnlyList<string> shapeNames, IReadOnlyList<string> variantNames,
        float listSpace)
    {
        GUILayout.BeginVertical(_theme.Card);
        _theme.SectionLabel("Hologram");

        string shapePlaceholder = device.HostShapeName.Length > 0 ? device.HostShapeName : WAITING_FOR_HOST;
        ObjectControlsUI.Result result = _controls.Draw(_theme, shapeNames, device.HostShapeIndex, shapePlaceholder,
            variantNames, device.HostVariantIndex, device.HostVariantName, device.IsHostSpinning, null, listSpace);

        if (result.PickedShape != RemoteProtocol.NO_INDEX && result.PickedShape != device.HostShapeIndex)
        {
            device.RequestSelectShape(result.PickedShape);
        }
        if (result.PickedVariant != RemoteProtocol.NO_INDEX && result.PickedVariant != device.HostVariantIndex)
        {
            device.RequestSelectVariant(result.PickedVariant);
        }
        if (result.Spinning != device.IsHostSpinning)
        {
            device.RequestSetSpinning(result.Spinning);
        }
        GUILayout.EndVertical();
    }

    private void DrawConnectionCard(RemoteLink link, RemoteDeviceSession device)
    {
        GUILayout.BeginVertical(_theme.Card);
        string text = link.LoadError ?? (device.Hint.Length > 0 ? device.Hint : WaitingText(device.State));
        GUILayout.Label(text, _theme.Paragraph);

        if (link.LoadError == null && device.CanReconnect)
        {
            GUILayout.Space(_theme.Gap);
            if (GUILayout.Button("Reconnect", _theme.AccentButton, GUILayout.Height(_theme.Row)))
            {
                device.Reconnect();
            }
        }
        GUILayout.EndVertical();
    }

    private static float ConnectedRows(bool hasSensors, bool hasText)
    {
        float after = 0f;
        if (hasSensors)
        {
            after += SENSORS_SECTION;
        }
        if (hasText)
        {
            after += TEXT_SECTION;
        }
        return HEADER + CARD + ObjectControlsUI.Rows(false) + Mathf.Max(after, TEXT_SECTION);
    }

    private static Rect SafeArea()
    {
        Rect safe = Screen.safeArea;
        return new Rect(safe.x, Screen.height - safe.yMax, safe.width, safe.height);
    }

    private static Color StatusColor(RemoteDeviceSession.DeviceState state)
    {
        switch (state)
        {
            case RemoteDeviceSession.DeviceState.Connected: return UITheme.GOOD;
            case RemoteDeviceSession.DeviceState.Failed: return UITheme.BAD;
            default: return UITheme.BUSY;
        }
    }

    private static string WaitingText(RemoteDeviceSession.DeviceState state)
    {
        return state == RemoteDeviceSession.DeviceState.Loading
            ? "Loading the connection library..."
            : "Connecting to the hologram. Keep the host page open.";
    }
}
