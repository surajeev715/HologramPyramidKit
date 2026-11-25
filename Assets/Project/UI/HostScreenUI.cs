using System.Collections.Generic;
using UnityEngine;

public class HostScreenUI
{
    private const float ROW_HEIGHT_CM = 1.4f;
    private const float MIN_ROW_HEIGHT_CM = 0.7f;
    private const float MARGIN_CM = 0.35f;
    private const float MENU_WIDTH_CM = 9f;
    private const float REMOTES_WIDTH_CM = 7f;

    private const float HEIGHT_SLACK = 1.04f;

    private const float DOUBLE_TAP_SECONDS = 0.4f;
    private const float DOUBLE_TAP_DISTANCE_CM = 1f;

    private const float MENU_SECONDS = 0.25f;
    private const float MENU_SLIDE_CM = 1.5f;
    private const float MAX_ANIMATION_STEP_SECONDS = 0.1f;

    private const float HINT_SECONDS = 4f;
    private const float HINT_FADE_SECONDS = 1f;
    private const float HINT_BOTTOM_CM = 0.8f;
    private static readonly GUIContent HINT = new GUIContent("Double-tap to open the menu");

    private const float ROWS_ABOVE_LIST = UITheme.PADDING + 1f + UITheme.GAP + 1f;

    private const int UI_DEPTH = -1;

    private readonly UITheme _theme = new UITheme();
    private readonly ShapeSwitchUI _shapeSwitchUI;
    private readonly SensorDebugUI _debugUI;

    private readonly RemoteLinkUI _remoteLinkUI;

    private readonly List<Rect> _panels = new List<Rect>();

    private bool _isMenuVisible;
    private bool _isDebugVisible;
    private float _menuAmount;
    private float _lastLayoutTime = float.NegativeInfinity;
    private bool _isHintDone;
    private float _hintStartTime = float.NegativeInfinity;
    private float _lastTapTime = float.NegativeInfinity;
    private Vector2 _lastTapPosition;

    public HostScreenUI(ShapeSwitchUI shapeSwitchUI, RemoteLinkUI remoteLinkUI, SensorDebugUI debugUI)
    {
        _shapeSwitchUI = shapeSwitchUI;
        _remoteLinkUI = remoteLinkUI;
        _debugUI = debugUI;
    }

    public void Draw(IShapeCycler cycler, bool spinning, TextContext textContext, RemoteLink remoteLink, SensorDebugData debugData)
    {
        GUI.depth = UI_DEPTH;
        GUI.color = Color.white;

        float pixelsPerCm = ScreenDpi.PixelsPerCm;
        float now = Time.unscaledTime;
        if (float.IsNegativeInfinity(_hintStartTime))
        {
            _hintStartTime = now;
        }

        if (Event.current.type == EventType.Layout)
        {
            float step = float.IsNegativeInfinity(_lastLayoutTime) ? 0f : Mathf.Clamp(now - _lastLayoutTime, 0f, MAX_ANIMATION_STEP_SECONDS);
            _lastLayoutTime = now;
            _menuAmount = Mathf.MoveTowards(_menuAmount, _isMenuVisible ? 1f : 0f, step / MENU_SECONDS);
        }

        bool isMenuDrawn = _menuAmount > 0f;
        bool isDebugVisible = _isDebugVisible;
        bool isPicking = _shapeSwitchUI.IsPicking;
        bool hasRemotes = _remoteLinkUI != null && remoteLink != null;
        _panels.Clear();

        float margin = MARGIN_CM * pixelsPerCm;
        float available = Screen.height - 2f * margin;
        float preferredRow = ROW_HEIGHT_CM * pixelsPerCm;
        float debugRows = isDebugVisible ? UITheme.GAP + SensorDebugUI.Rows() : 0f;
        float rows = MenuRows() + debugRows;
        float row = Mathf.Clamp(available / (rows * HEIGHT_SLACK), MIN_ROW_HEIGHT_CM * pixelsPerCm, preferredRow);
        _theme.Prepare(row);

        float listSpace = available - (ROWS_ABOVE_LIST + UITheme.PADDING + debugRows) * row * HEIGHT_SLACK;

        float scale = row / preferredRow;
        float menuWidth = Mathf.Min(MENU_WIDTH_CM * pixelsPerCm * scale, Screen.width - 2f * margin);
        float remotesWidth = Mathf.Min(REMOTES_WIDTH_CM * pixelsPerCm * scale, Screen.width - 2f * margin);

        bool hasRemotesColumn = hasRemotes && Screen.width >= menuWidth + remotesWidth + 3f * margin;

        if (isMenuDrawn)
        {
            float eased = UITheme.Ease(_menuAmount);
            float slide = (1f - eased) * MENU_SLIDE_CM * pixelsPerCm;
            Color previous = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, eased);

            Rect left = new Rect(margin - slide, margin, menuWidth, available);
            GUILayout.BeginArea(left);
            DrawMenu(cycler, spinning, textContext, remoteLink, isDebugVisible, isPicking, hasRemotes && !hasRemotesColumn,
                menuWidth, listSpace);
            AddPanel(left);
            GUILayout.EndArea();

            if (hasRemotesColumn)
            {
                Rect right = new Rect(Screen.width - margin - remotesWidth + slide, margin, remotesWidth, available);
                GUILayout.BeginArea(right);
                GUILayout.BeginVertical(_theme.Panel);
                _remoteLinkUI.Draw(_theme, remoteLink, remotesWidth - 2f * _theme.Padding);
                GUILayout.EndVertical();
                AddPanel(right);
                GUILayout.EndArea();
            }
            GUI.color = previous;
        }

        if (isDebugVisible)
        {
            float debugHeight = SensorDebugUI.Rows() * row * HEIGHT_SLACK;
            Rect debug = new Rect(margin, Screen.height - margin - debugHeight, menuWidth, debugHeight);
            GUILayout.BeginArea(debug);
            GUILayout.FlexibleSpace();
            _debugUI.Draw(_theme, debugData);
            AddPanel(debug);
            GUILayout.EndArea();
        }

        if (!_isMenuVisible)
        {
            DrawHint(now, pixelsPerCm);
        }

        HandleDoubleTap(now, pixelsPerCm);
    }

    private void DrawMenu(IShapeCycler cycler, bool spinning, TextContext textContext, RemoteLink remoteLink,
        bool isDebugVisible, bool isPicking, bool hasRemotes, float menuWidth, float listSpace)
    {
        GUILayoutOption rowHeight = GUILayout.Height(_theme.Row);

        GUILayout.BeginVertical(_theme.Panel);
        GUILayout.BeginHorizontal(rowHeight);
        GUILayout.Label("Hologram", _theme.Title, rowHeight);
        GUILayout.FlexibleSpace();
        GUILayout.Label("double-tap to hide", _theme.CaptionRight, rowHeight);
        GUILayout.EndHorizontal();
        _theme.Divider();

        _shapeSwitchUI.Draw(_theme, cycler, spinning, textContext, listSpace);

        if (!isPicking)
        {
            GUILayout.Space(_theme.Gap);
            _theme.SectionLabel("Settings");
            _isDebugVisible = _theme.SwitchRow("Sensor debug", isDebugVisible);

            if (hasRemotes)
            {
                GUILayout.Space(_theme.Gap);
                _remoteLinkUI.Draw(_theme, remoteLink, menuWidth - 2f * _theme.Padding);
            }
        }
        GUILayout.EndVertical();
    }

    private static float MenuRows()
    {
        const float title = 1f + UITheme.GAP;
        const float settings = UITheme.GAP + UITheme.SECTION_LABEL + 1f;
        return 2f * UITheme.PADDING + title + ShapeSwitchUI.Rows() + settings;
    }

    private void DrawHint(float now, float pixelsPerCm)
    {
        if (_isHintDone)
        {
            return;
        }

        float age = now - _hintStartTime;
        float alpha = 1f - Mathf.Clamp01((age - HINT_SECONDS) / HINT_FADE_SECONDS);
        if (alpha <= 0f)
        {
            _isHintDone = true;
            return;
        }

        Vector2 size = _theme.Hint.CalcSize(HINT);
        Rect rect = new Rect((Screen.width - size.x) / 2f, Screen.height - HINT_BOTTOM_CM * pixelsPerCm - size.y, size.x, size.y);

        Color previous = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(rect, HINT, _theme.Hint);
        GUI.color = previous;
    }

    private void HandleDoubleTap(float now, float pixelsPerCm)
    {
        Event current = Event.current;
        if (current.type != EventType.MouseDown)
        {
            return;
        }

        foreach (Rect panel in _panels)
        {
            if (panel.Contains(current.mousePosition))
            {
                return;
            }
        }

        bool isQuick = now - _lastTapTime <= DOUBLE_TAP_SECONDS;
        bool isClose = (current.mousePosition - _lastTapPosition).magnitude <= DOUBLE_TAP_DISTANCE_CM * pixelsPerCm;
        if (!isQuick || !isClose)
        {
            _lastTapTime = now;
            _lastTapPosition = current.mousePosition;
            return;
        }

        _lastTapTime = float.NegativeInfinity;
        _isMenuVisible = !_isMenuVisible;
        _isHintDone = true;
        if (!_isMenuVisible)
        {
            _shapeSwitchUI.CloseLists();
        }
        current.Use();
    }

    private void AddPanel(Rect area)
    {
        if (Event.current.type == EventType.Layout)
        {
            return;
        }
        Rect panel = GUILayoutUtility.GetLastRect();
        panel.position += area.position;
        _panels.Add(panel);
    }
}
