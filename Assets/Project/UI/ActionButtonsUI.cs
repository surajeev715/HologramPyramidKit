using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ActionButtonsUI
{


    private static readonly Color INDICATOR_ON = new Color(1f, 0.85f, 0.2f);

    private GUIStyle _labelStyle;

    public void Draw(GUILayoutOption height, ActionController controller, GUIStyle buttonStyle, GUIStyle labelStyle)
    {
        GUILayout.Label("Sensors of this device (lit when detected, tap to trigger)", labelStyle);

        float now = Time.time;
        GUILayout.BeginHorizontal();
        if (SensorButton("Clap", controller.IsClapActive(), height, buttonStyle)) controller.TriggerClap(true, now);
        if (SensorButton("Blow", controller.IsBlowActive(), height, buttonStyle)) controller.TriggerBlow(true, now);
        if (SensorButton("Light", controller.IsLightActive(), height, buttonStyle)) controller.TriggerLight(true, now);
        GUILayout.EndHorizontal();
    }

    private bool SensorButton(string text, bool hasIndicatorOn,  GUILayoutOption height, GUIStyle buttonStyle)
    {
        Color previous = GUI.backgroundColor;
        if (hasIndicatorOn)
        {
            GUI.backgroundColor = INDICATOR_ON;
        }
        bool isPressed = GUILayout.Button(text, buttonStyle, height);
        GUI.backgroundColor = previous;
        return isPressed;
    }
}
