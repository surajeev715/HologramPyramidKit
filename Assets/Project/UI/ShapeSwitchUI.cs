using UnityEngine;

public class ShapeSwitchUI
{
    [Header("Buttons")]
    public float buttonWidthCm = 3.5f;
    public float buttonHeightCm = 1f;
    public float buttonMarginCm = 0.3f;

    private GUIStyle buttonStyle;

    private ShapeSwitcher _switcher;

    private bool _cycleNextPressed = false;

    public ShapeSwitchUI(ShapeSwitcher switcher)
    {
        _switcher = switcher;
    }

    public void Draw(IShapeCycler cycler, bool spinning)
    {
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

        // Shape Button (Klick führt NextShape aus)
        if (GUI.Button(shapeRect, _switcher.getCurrentShapeName(), buttonStyle))
        {
            _switcher.NextShape();
        }

        // Spin Button
        if (GUI.Button(spinRect, spinning ? "Spin: On" : "Spin: Off", buttonStyle))
        {
            _switcher.SetSpinning(!spinning);
        }


        if (cycler != null && cycler.Count > 1)
        {
            Rect animationRect = new Rect(margin, shapeRect.y - height - margin, width, height);

            if (GUI.Button(animationRect, cycler.CurrentName, buttonStyle))
            {
                _cycleNextPressed = true;
            }
        }
    }

    public bool ConsumeCycleNextPressed()
    {
        bool pressed = _cycleNextPressed;
        _cycleNextPressed = false;
        return pressed;
    }
}
