using System;
using TMPro;
using UnityEngine;

public class TextRenderingScript : MonoBehaviour
{
    private const string STARTING_TEXT = "WRITE SOMETHING I got a script :)";

    private const float DEFAULT_SHAPE_RADIUS = 0.71f;
    private const float DEFAULT_SHAPE_HALF_HEIGHT = 0.5f;

    private static readonly char[] WORD_SEPARATORS = { ' ', '\t', '\n', '\r' };

    [Tooltip("Smallest font size. A text that does not fit even at this size is cut off.")]
    public float minFontSize = 0.1f;

    [Tooltip("Largest font size, used for very short texts.")]
    public float maxFontSize = 20f;

    [Tooltip("Share of the hologram's space the text may use, so it keeps a little distance from the edges.")]
    [Range(0.5f, 1f)]
    public float fillShare = 0.9f;

    private TextMeshPro _text;
    private string _shownText;
    private bool _isShownWordPerLine;

    void Start()
    {
        Prepare();
        ShowFitted(STARTING_TEXT);
    }

    public void UpdateText(string newText, bool isWordPerLine)
    {
        Prepare();

        string text = newText ?? "";
        if (text == _shownText && isWordPerLine == _isShownWordPerLine)
        {
            return;
        }
        _shownText = text;
        _isShownWordPerLine = isWordPerLine;

        ShowFitted(isWordPerLine ? OneWordPerLine(text) : text);
        Debug.Log($"Update Text has been called new value: {text}, one word per line: {isWordPerLine}");
    }

    private void ShowFitted(string text)
    {
        _text.enableAutoSizing = true;
        _text.SetText(text);
        _text.ForceMeshUpdate();

        float fittedSize = _text.fontSize;
        _text.enableAutoSizing = false;
        _text.fontSize = fittedSize;
    }

    private void Prepare()
    {
        if (_text != null)
        {
            return;
        }
        _text = GetComponentInChildren<TextMeshPro>();
        FitIntoHologram();
    }

    private void FitIntoHologram()
    {
        Hologram hologram = FindAnyObjectByType<Hologram>();
        float radius = hologram != null ? hologram.shapeRadius : DEFAULT_SHAPE_RADIUS;
        float halfHeight = hologram != null ? hologram.shapeHalfHeight : DEFAULT_SHAPE_HALF_HEIGHT;

        Vector3 scale = _text.transform.lossyScale;
        _text.rectTransform.sizeDelta = new Vector2(
            2f * radius * fillShare / Mathf.Abs(scale.x),
            2f * halfHeight * fillShare / Mathf.Abs(scale.y));
        _text.margin = Vector4.zero;

        _text.richText = false;

        _text.fontSizeMin = minFontSize;
        _text.fontSizeMax = maxFontSize;
        _text.textWrappingMode = TextWrappingModes.Normal;
        _text.alignment = TextAlignmentOptions.Center;
    }

    private static string OneWordPerLine(string text)
    {
        return string.Join("\n", text.Split(WORD_SEPARATORS, StringSplitOptions.RemoveEmptyEntries));
    }
}
