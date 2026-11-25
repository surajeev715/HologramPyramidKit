using UnityEngine;

public struct SensorDebugData
{
    public float Peak;
    public float HeldPeak;
    public float Rms;
    public float HeldRms;
    public float Crest;
    public float CrestAtPeak;
    public float LastDuration;
    public float Gate;
    public float Brightness;
    public SensorContext Context;
}

public class SensorDebugUI
{
    private const float ROW_HEIGHT_IN_ROWS = 0.5f;
    private const float FLAG_WIDTH_IN_ROWS = 2.2f;
    private const int VALUE_LINES = 6;
    private const int FLAG_LINES = 2;

    public static float Rows()
    {
        return 2f * UITheme.PADDING + UITheme.SECTION_LABEL + UITheme.GAP / 2f
            + (VALUE_LINES + FLAG_LINES) * ROW_HEIGHT_IN_ROWS;
    }

    public void Draw(UITheme theme, SensorDebugData data)
    {
        float height = theme.Row * ROW_HEIGHT_IN_ROWS;

        GUILayout.BeginVertical(theme.Panel);
        theme.SectionLabel("Sensor debug");
        ValueLine(theme, height, "Peak", $"{data.Peak:F3}  (max 2 s {data.HeldPeak:F3})");
        ValueLine(theme, height, "RMS", $"{data.Rms:F3}  (max 2 s {data.HeldRms:F3})");
        ValueLine(theme, height, "Crest", $"{data.Crest:F1}  (at max peak {data.CrestAtPeak:F1})");
        ValueLine(theme, height, "Last duration", $"{data.LastDuration:F3} s");
        ValueLine(theme, height, "Gate", $"{data.Gate:F3}");
        ValueLine(theme, height, "Brightness", $"{data.Brightness:F3}");

        GUILayout.Space(theme.Gap / 2f);
        GUILayout.BeginHorizontal();
        Flag(theme, height, "Clap", data.Context.HasClap);
        Flag(theme, height, "Blow", data.Context.HasBlow);
        Flag(theme, height, "Light", data.Context.HasLight);
        GUILayout.EndHorizontal();
        Flag(theme, height, "Calibrating", data.Context.IsCalibrating);
        GUILayout.EndVertical();
    }

    private static void ValueLine(UITheme theme, float height, string label, string value)
    {
        GUILayout.BeginHorizontal(GUILayout.Height(height));
        GUILayout.Label(label, theme.Caption, GUILayout.Height(height));
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, theme.CaptionRight, GUILayout.Height(height));
        GUILayout.EndHorizontal();
    }

    private static void Flag(UITheme theme, float height, string label, bool isOn)
    {
        GUILayout.BeginVertical(GUILayout.Width(theme.Row * FLAG_WIDTH_IN_ROWS));
        theme.StatusLine(label, isOn ? UITheme.ACCENT : UITheme.OFF, theme.Caption, height);
        GUILayout.EndVertical();
    }
}
