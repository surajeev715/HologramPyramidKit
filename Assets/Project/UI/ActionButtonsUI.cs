using UnityEngine;

public class ActionButtonsUI
{
    public static bool HasButtons(ShapeFeatures features)
    {
        return (features & (ShapeFeatures.Clap | ShapeFeatures.Blow | ShapeFeatures.Light)) != 0;
    }

    public void Draw(UITheme theme, ActionController controller, ShapeFeatures features)
    {
        float now = Time.time;
        GUILayoutOption height = GUILayout.Height(theme.Row);
        bool isFirst = true;

        GUILayout.BeginHorizontal();
        if ((features & ShapeFeatures.Clap) != 0 && SensorButton(theme, "Clap", controller.IsClapActive(), height, ref isFirst))
        {
            controller.TriggerClap(true, now);
        }
        if ((features & ShapeFeatures.Blow) != 0 && SensorButton(theme, "Blow", controller.IsBlowActive(), height, ref isFirst))
        {
            controller.TriggerBlow(true, now);
        }
        if ((features & ShapeFeatures.Light) != 0 && SensorButton(theme, "Light", controller.IsLightActive(), height, ref isFirst))
        {
            controller.TriggerLight(true, now);
        }
        GUILayout.EndHorizontal();
    }

    private static bool SensorButton(UITheme theme, string text, bool isLit, GUILayoutOption height, ref bool isFirst)
    {
        if (!isFirst)
        {
            GUILayout.Space(theme.Gap);
        }
        isFirst = false;
        return GUILayout.Button(text, isLit ? theme.AccentButton : theme.Button, height, GUILayout.ExpandWidth(true));
    }
}
