using System.Collections.Generic;
using UnityEngine;

public class UITheme
{
    public static readonly Color ACCENT = new Color(0.96f, 0.96f, 0.97f);
    public static readonly Color TEXT = new Color(0.93f, 0.93f, 0.94f);
    public static readonly Color MUTED = new Color(0.6f, 0.6f, 0.63f);
    public static readonly Color ON_ACCENT = new Color(0.06f, 0.06f, 0.07f);
    public static readonly Color OFF = new Color(1f, 1f, 1f, 0.2f);
    public static readonly Color GOOD = new Color(0.3f, 0.9f, 0.55f);
    public static readonly Color BUSY = new Color(1f, 0.75f, 0.3f);
    public static readonly Color BAD = new Color(1f, 0.42f, 0.42f);

    private static readonly Color PANEL_FILL = new Color(0.07f, 0.07f, 0.08f, 0.9f);
    private static readonly Color PANEL_STROKE = new Color(1f, 1f, 1f, 0.16f);
    private static readonly Color CARD_FILL = new Color(1f, 1f, 1f, 0.035f);
    private static readonly Color CARD_STROKE = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color BUTTON_FILL = new Color(1f, 1f, 1f, 0.07f);
    private static readonly Color BUTTON_HOVER = new Color(1f, 1f, 1f, 0.11f);
    private static readonly Color BUTTON_STROKE = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color BUTTON_PRESSED = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color ACCENT_HOVER = new Color(1f, 1f, 1f);
    private static readonly Color ACCENT_PRESSED = new Color(0.78f, 0.78f, 0.8f);
    private static readonly Color SELECTED_FILL = new Color(1f, 1f, 1f, 0.14f);
    private static readonly Color FIELD_FILL = new Color(0f, 0f, 0f, 0.4f);
    private static readonly Color HINT_FILL = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color CLEAR = new Color(1f, 1f, 1f, 0f);
    private static readonly Color DIVIDER = new Color(1f, 1f, 1f, 0.12f);
    private static readonly Color BACKGROUND_TOP = new Color(0.11f, 0.11f, 0.12f);
    private static readonly Color BACKGROUND_BOTTOM = new Color(0f, 0f, 0f);

    public const float GAP = 0.28f;
    public const float PADDING = 0.32f;
    public const float SECTION_LABEL = 0.6f;
    private const float CORNER = 0.28f;
    private const float FONT_BODY = 0.38f;
    private const float FONT_TITLE = 0.5f;
    private const float FONT_CAPTION = 0.3f;
    private const float FONT_SECTION = 0.27f;
    private const float ICON = 0.3f;
    private const float SWITCH_HEIGHT = 0.5f;
    private const float SWITCH_WIDTH = 0.9f;
    private const float KNOB_INSET = 0.06f;
    private const float DOT = 0.22f;

    private const float SWITCH_SECONDS = 0.15f;

    private const float MAX_ANIMATION_STEP_SECONDS = 0.1f;

    private const float STROKE_PIXELS = 1f;
    private const int ICON_PIXELS = 48;
    private const int ICON_SUPERSAMPLING = 4;
    private const int BACKGROUND_PIXELS = 64;

    public float Row { get; private set; }
    public float Gap => Row * GAP;
    public float Padding => Row * PADDING;
    public float SectionLabelHeight => Row * SECTION_LABEL;

    public GUIStyle Panel { get; private set; }
    public GUIStyle Card { get; private set; }
    public GUIStyle Button { get; private set; }
    public GUIStyle AccentButton { get; private set; }
    public GUIStyle ListItem { get; private set; }
    public GUIStyle SelectedListItem { get; private set; }
    public GUIStyle TextField { get; private set; }
    public GUIStyle Title { get; private set; }
    public GUIStyle Body { get; private set; }
    public GUIStyle Value { get; private set; }
    public GUIStyle Caption { get; private set; }
    public GUIStyle Paragraph { get; private set; }
    public GUIStyle CaptionRight { get; private set; }
    public GUIStyle Section { get; private set; }
    public GUIStyle SectionNote { get; private set; }
    public GUIStyle Hint { get; private set; }

    private readonly List<Texture2D> _textures = new List<Texture2D>();
    private readonly Dictionary<int, GUIStyle> _circles = new Dictionary<int, GUIStyle>();
    private GUIStyle _rounded;
    private Texture2D _chevronDown;
    private Texture2D _chevronRight;
    private Texture2D _background;
    private int _corner = -1;
    private int _padding = -1;

    private float _lastRepaintTime = float.NegativeInfinity;
    private float _repaintStep;
    private readonly Dictionary<string, float> _switchPositions = new Dictionary<string, float>();

    public void Prepare(float rowHeight)
    {
        Row = Mathf.Max(1f, rowHeight);

        if (Event.current.type == EventType.Repaint)
        {
            float now = Time.unscaledTime;
            _repaintStep = float.IsNegativeInfinity(_lastRepaintTime) ? 0f : Mathf.Clamp(now - _lastRepaintTime, 0f, MAX_ANIMATION_STEP_SECONDS);
            _lastRepaintTime = now;
        }

        int corner = Mathf.Max(2, Mathf.RoundToInt(Row * CORNER));
        if (corner != _corner)
        {
            BuildStyles(corner);
        }

        int body = Mathf.RoundToInt(Row * FONT_BODY);
        int caption = Mathf.RoundToInt(Row * FONT_CAPTION);
        Button.fontSize = body;
        AccentButton.fontSize = body;
        ListItem.fontSize = body;
        SelectedListItem.fontSize = body;
        TextField.fontSize = body;
        Body.fontSize = body;
        Value.fontSize = body;
        Title.fontSize = Mathf.RoundToInt(Row * FONT_TITLE);
        Caption.fontSize = caption;
        Paragraph.fontSize = caption;
        CaptionRight.fontSize = caption;
        Hint.fontSize = caption;
        Section.fontSize = Mathf.RoundToInt(Row * FONT_SECTION);
        SectionNote.fontSize = Section.fontSize;

        int padding = Mathf.RoundToInt(Padding);
        if (padding == _padding)
        {
            return;
        }
        _padding = padding;
        Panel.padding = new RectOffset(padding, padding, padding, padding);
        Card.padding = new RectOffset(padding, padding, padding, padding);
        RectOffset textPadding = new RectOffset(padding, padding, 0, 0);
        Button.padding = textPadding;
        AccentButton.padding = textPadding;
        ListItem.padding = textPadding;
        SelectedListItem.padding = textPadding;
        TextField.padding = textPadding;
        Hint.padding = new RectOffset(padding * 2, padding * 2, padding, padding);
    }

    public float AnimationStep => Event.current.type == EventType.Repaint ? _repaintStep : 0f;

    public float Animate(float current, float target, float seconds)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return current;
        }
        return Mathf.MoveTowards(current, target, seconds > 0f ? _repaintStep / seconds : 1f);
    }

    public static float Ease(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    public void DrawBackground()
    {
        if (Event.current.type == EventType.Repaint)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _background, ScaleMode.StretchToFill);
        }
    }

    public void SectionLabel(string text)
    {
        GUILayout.Label(text.ToUpperInvariant(), Section, GUILayout.Height(SectionLabelHeight));
    }

    public void Divider()
    {
        Rect row = GUILayoutUtility.GetRect(0f, Gap, GUILayout.ExpandWidth(true), GUILayout.Height(Gap));
        if (Event.current.type == EventType.Repaint)
        {
            Color previous = GUI.color;
            GUI.color = previous * DIVIDER;
            GUI.DrawTexture(new Rect(row.x, Mathf.Round(row.center.y), row.width, 1f), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }

    public void SectionLabel(string text, string note)
    {
        GUILayoutOption height = GUILayout.Height(SectionLabelHeight);
        GUILayout.BeginHorizontal(height);
        GUILayout.Label(text.ToUpperInvariant(), Section, height);
        GUILayout.FlexibleSpace();
        GUILayout.Label(note, SectionNote, height);
        GUILayout.EndHorizontal();
    }

    public bool SwitchRow(string label, bool value)
    {
        Rect row = GUILayoutUtility.GetRect(0f, Row, GUILayout.ExpandWidth(true), GUILayout.Height(Row));
        bool isClicked = GUI.Button(row, GUIContent.none, Button);

        float switchWidth = Row * SWITCH_WIDTH;
        float switchHeight = Row * SWITCH_HEIGHT;
        Rect track = new Rect(row.xMax - Padding - switchWidth, row.center.y - switchHeight / 2f, switchWidth, switchHeight);
        GUI.Label(new Rect(row.x + Padding, row.y, track.x - row.x - 2f * Padding, row.height), label, Body);

        float target = value ? 1f : 0f;
        float position = _switchPositions.TryGetValue(label, out float stored) ? stored : target;
        position = Animate(position, target, SWITCH_SECONDS);
        _switchPositions[label] = position;

        if (Event.current.type == EventType.Repaint)
        {
            float eased = Ease(position);
            float inset = Row * KNOB_INSET;
            float knob = switchHeight - 2f * inset;
            float knobX = Mathf.Lerp(track.x + inset, track.xMax - inset - knob, eased);
            DrawCircle(track, Color.Lerp(OFF, ACCENT, eased));
            DrawCircle(new Rect(knobX, track.y + inset, knob, knob), Color.Lerp(TEXT, ON_ACCENT, eased));
        }
        return isClicked ? !value : value;
    }

    public bool ValueRow(string label, string value)
    {
        Rect row = GUILayoutUtility.GetRect(0f, Row, GUILayout.ExpandWidth(true), GUILayout.Height(Row));
        bool isClicked = GUI.Button(row, GUIContent.none, Button);

        Rect icon = IconRect(row);
        float labelWidth = (icon.x - row.x - 2f * Padding) * 0.45f;
        GUI.Label(new Rect(row.x + Padding, row.y, labelWidth, row.height), label, Body);
        GUI.Label(new Rect(row.x + Padding + labelWidth, row.y, icon.x - row.x - 2f * Padding - labelWidth, row.height), value, Value);
        DrawIcon(icon, _chevronRight, 0f, MUTED);
        return isClicked;
    }

    public void DrawDropdownArrow(Rect row, float openAmount)
    {
        float eased = Ease(openAmount);
        DrawIcon(IconRect(row), _chevronDown, eased * 180f, Color.Lerp(MUTED, ACCENT, eased));
    }

    public Rect IconRect(Rect row)
    {
        float size = Row * ICON;
        return new Rect(row.xMax - Padding - size, row.center.y - size / 2f, size, size);
    }

    public void StatusLine(string text, Color color, GUIStyle style, float height)
    {
        Rect row = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true), GUILayout.Height(height));
        float dot = Row * DOT;
        if (Event.current.type == EventType.Repaint)
        {
            DrawCircle(new Rect(row.x, row.center.y - dot / 2f, dot, dot), color);
        }
        GUI.Label(new Rect(row.x + dot * 2f, row.y, row.width - dot * 2f, row.height), text, style);
    }

    public void DrawRounded(Rect rect, Color color)
    {
        DrawTinted(_rounded, rect, color);
    }

    public void DrawCircle(Rect rect, Color color)
    {
        int radius = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(rect.width, rect.height) / 2f));
        if (!_circles.TryGetValue(radius, out GUIStyle style))
        {
            style = new GUIStyle();
            style.normal.background = CreateRounded(radius, Color.white, CLEAR);
            style.border = new RectOffset(radius, radius, radius, radius);
            _circles[radius] = style;
        }
        DrawTinted(style, rect, color);
    }

    private static void DrawTinted(GUIStyle style, Rect rect, Color color)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }
        Color previous = GUI.backgroundColor;
        GUI.backgroundColor = color;
        style.Draw(rect, false, false, false, false);
        GUI.backgroundColor = previous;
    }

    private void DrawIcon(Rect rect, Texture2D icon, float rotationInDegrees, Color color)
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }
        Color previousColor = GUI.color;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.color = previousColor * color;
        if (rotationInDegrees != 0f)
        {
            GUIUtility.RotateAroundPivot(rotationInDegrees, rect.center);
        }
        GUI.DrawTexture(rect, icon);
        GUI.matrix = previousMatrix;
        GUI.color = previousColor;
    }

    private void BuildStyles(int corner)
    {
        foreach (Texture2D texture in _textures)
        {
            Object.Destroy(texture);
        }
        _textures.Clear();
        _circles.Clear();
        _corner = corner;
        _padding = -1;

        RectOffset border = new RectOffset(corner, corner, corner, corner);
        Font font = GUI.skin.font;

        _rounded = Box(CreateRounded(corner, Color.white, CLEAR), border);
        Panel = Box(CreateRounded(corner, PANEL_FILL, PANEL_STROKE), border);
        Card = Box(CreateRounded(corner, CARD_FILL, CARD_STROKE), border);

        Button = Clickable(font, border, TextAnchor.MiddleCenter, TEXT,
            CreateRounded(corner, BUTTON_FILL, BUTTON_STROKE),
            CreateRounded(corner, BUTTON_HOVER, PANEL_STROKE),
            CreateRounded(corner, BUTTON_PRESSED, ACCENT));

        AccentButton = Clickable(font, border, TextAnchor.MiddleCenter, ON_ACCENT,
            CreateRounded(corner, ACCENT, CLEAR),
            CreateRounded(corner, ACCENT_HOVER, CLEAR),
            CreateRounded(corner, ACCENT_PRESSED, CLEAR));
        AccentButton.fontStyle = FontStyle.Bold;

        Texture2D clear = CreateRounded(corner, CLEAR, CLEAR);
        ListItem = Clickable(font, border, TextAnchor.MiddleLeft, TEXT,
            clear,
            CreateRounded(corner, BUTTON_FILL, CLEAR),
            CreateRounded(corner, BUTTON_PRESSED, CLEAR));

        Texture2D selected = CreateRounded(corner, SELECTED_FILL, PANEL_STROKE);
        SelectedListItem = Clickable(font, border, TextAnchor.MiddleLeft, ACCENT, selected, selected, selected);
        SelectedListItem.fontStyle = FontStyle.Bold;

        TextField = new GUIStyle(GUI.skin.textField);
        TextField.font = font;
        TextField.border = border;
        TextField.alignment = TextAnchor.MiddleLeft;
        TextField.clipping = TextClipping.Clip;
        TextField.wordWrap = false;
        TextField.richText = false;
        TextField.margin = new RectOffset();
        Texture2D field = CreateRounded(corner, FIELD_FILL, BUTTON_STROKE);
        Texture2D focused = CreateRounded(corner, FIELD_FILL, ACCENT);
        TextField.normal.background = field;
        TextField.hover.background = field;
        TextField.active.background = focused;
        TextField.focused.background = focused;
        TextField.onNormal.background = focused;
        TextField.onFocused.background = focused;
        TextField.normal.textColor = TEXT;
        TextField.hover.textColor = TEXT;
        TextField.active.textColor = TEXT;
        TextField.focused.textColor = TEXT;

        Title = Text(font, TextAnchor.MiddleLeft, TEXT);
        Title.fontStyle = FontStyle.Bold;
        Body = Text(font, TextAnchor.MiddleLeft, TEXT);
        Value = Text(font, TextAnchor.MiddleRight, TEXT);
        Value.fontStyle = FontStyle.Bold;
        Caption = Text(font, TextAnchor.MiddleLeft, MUTED);
        Paragraph = Text(font, TextAnchor.UpperLeft, MUTED);
        Paragraph.wordWrap = true;
        CaptionRight = Text(font, TextAnchor.MiddleRight, MUTED);
        Section = Text(font, TextAnchor.LowerLeft, MUTED);
        Section.fontStyle = FontStyle.Bold;
        SectionNote = Text(font, TextAnchor.LowerRight, MUTED);

        Hint = Box(CreateRounded(corner, HINT_FILL, CARD_STROKE), border);
        Hint.font = font;
        Hint.alignment = TextAnchor.MiddleCenter;
        Hint.normal.textColor = TEXT;

        _chevronDown = CreateTriangle(new Vector2(0.12f, 0.72f), new Vector2(0.88f, 0.72f), new Vector2(0.5f, 0.28f));
        _chevronRight = CreateTriangle(new Vector2(0.28f, 0.12f), new Vector2(0.28f, 0.88f), new Vector2(0.72f, 0.5f));
        _background = CreateGradient(BACKGROUND_TOP, BACKGROUND_BOTTOM);
    }

    private static GUIStyle Box(Texture2D background, RectOffset border)
    {
        GUIStyle style = new GUIStyle();
        style.normal.background = background;
        style.border = border;
        return style;
    }

    private static GUIStyle Clickable(Font font, RectOffset border, TextAnchor alignment, Color textColor,
        Texture2D normal, Texture2D hover, Texture2D pressed)
    {
        GUIStyle style = Text(font, alignment, textColor);
        style.border = border;
        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = pressed;
        style.focused.background = normal;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        style.focused.textColor = textColor;
        return style;
    }

    private static GUIStyle Text(Font font, TextAnchor alignment, Color color)
    {
        GUIStyle style = new GUIStyle();
        style.font = font;
        style.alignment = alignment;
        style.normal.textColor = color;
        style.richText = false;
        style.wordWrap = false;
        style.clipping = TextClipping.Clip;
        return style;
    }

    private Texture2D CreateRounded(int radius, Color fill, Color stroke)
    {
        int size = radius * 2 + 2;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float centerX = x + 0.5f;
                float centerY = y + 0.5f;
                float dx = Mathf.Max(radius - centerX, 0f, centerX - (size - radius));
                float dy = Mathf.Max(radius - centerY, 0f, centerY - (size - radius));

                float inside = radius - Mathf.Sqrt(dx * dx + dy * dy);
                float coverage = Mathf.Clamp01(inside + 0.5f);

                float strokeAlpha = stroke.a * Mathf.Clamp01(STROKE_PIXELS + 0.5f - inside);
                float alpha = strokeAlpha + fill.a * (1f - strokeAlpha);
                Color color = alpha > 0f
                    ? (stroke * strokeAlpha + fill * fill.a * (1f - strokeAlpha)) / alpha
                    : fill;
                color.a = alpha * coverage;
                pixels[y * size + x] = color;
            }
        }
        return CreateTexture(size, size, pixels, FilterMode.Bilinear);
    }

    private Texture2D CreateTriangle(Vector2 a, Vector2 b, Vector2 c)
    {
        Color32[] pixels = new Color32[ICON_PIXELS * ICON_PIXELS];
        float step = 1f / (ICON_PIXELS * ICON_SUPERSAMPLING);
        for (int y = 0; y < ICON_PIXELS; y++)
        {
            for (int x = 0; x < ICON_PIXELS; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ICON_SUPERSAMPLING; sy++)
                {
                    for (int sx = 0; sx < ICON_SUPERSAMPLING; sx++)
                    {
                        Vector2 point = new Vector2(
                            (x * ICON_SUPERSAMPLING + sx + 0.5f) * step,
                            (y * ICON_SUPERSAMPLING + sy + 0.5f) * step);
                        if (IsInTriangle(point, a, b, c))
                        {
                            hits++;
                        }
                    }
                }
                float alpha = hits / (float)(ICON_SUPERSAMPLING * ICON_SUPERSAMPLING);
                pixels[y * ICON_PIXELS + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        return CreateTexture(ICON_PIXELS, ICON_PIXELS, pixels, FilterMode.Bilinear);
    }

    private Texture2D CreateGradient(Color top, Color bottom)
    {
        Color32[] pixels = new Color32[BACKGROUND_PIXELS];
        for (int y = 0; y < BACKGROUND_PIXELS; y++)
        {
            pixels[y] = Color.Lerp(bottom, top, y / (float)(BACKGROUND_PIXELS - 1));
        }
        return CreateTexture(1, BACKGROUND_PIXELS, pixels, FilterMode.Bilinear);
    }

    private static bool IsInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(p, a, b);
        float d2 = Cross(p, b, c);
        float d3 = Cross(p, c, a);
        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float Cross(Vector2 p, Vector2 a, Vector2 b)
    {
        return (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
    }

    private Texture2D CreateTexture(int width, int height, Color32[] pixels, FilterMode filter)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = filter;
        texture.SetPixels32(pixels);
        texture.Apply();
        _textures.Add(texture);
        return texture;
    }
}
