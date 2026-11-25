using System.Collections.Generic;
using UnityEngine;

public class DropdownUI
{
    public const float ITEM_HEIGHT_IN_ROWS = 0.9f;

    private const float LABEL_SHARE = 0.4f;

    private const float ARROW_SECONDS = 0.2f;
    private const float LIST_FADE_SECONDS = 0.18f;

    private const int MIN_VISIBLE_ITEMS = 2;

    private const float DRAG_THRESHOLD_IN_ROWS = 0.15f;

    private const float GLIDE_SLOWDOWN_PER_SECOND = 5f;
    private const float MIN_GLIDE_ROWS_PER_SECOND = 0.5f;

    private const float MAX_PAUSE_BEFORE_GLIDE_SECONDS = 0.08f;

    private const float WHEEL_ITEMS_PER_STEP = 0.35f;

    private const float SCROLLBAR_WIDTH_IN_ROWS = 0.08f;
    private const float MIN_SCROLLBAR_IN_ROWS = 0.5f;
    private static readonly Color SCROLLBAR = new Color(1f, 1f, 1f, 0.35f);

    private float _openAmount;
    private float _listAlpha;

    private float _scroll;
    private float _velocity;
    private bool _isScrollToSelectedNeeded;
    private bool _isPressed;
    private bool _isDragging;
    private float _pressY;
    private float _pressScroll;
    private float _lastDragY;
    private float _lastDragTime;

    public bool IsOpen { get; private set; }

    public void Close()
    {
        IsOpen = false;
    }

    public int Draw(UITheme theme, string label, IReadOnlyList<string> options, int selected, string placeholder,
        float maxListHeight)
    {
        bool isOpen = IsOpen;
        bool hasOptions = options != null && options.Count > 0;
        string current = hasOptions && selected >= 0 && selected < options.Count ? options[selected] : placeholder;

        Rect header = GUILayoutUtility.GetRect(0f, theme.Row, GUILayout.ExpandWidth(true), GUILayout.Height(theme.Row));
        GUI.enabled = hasOptions;
        if (GUI.Button(header, GUIContent.none, theme.Button))
        {
            IsOpen = !IsOpen;
            _isScrollToSelectedNeeded = IsOpen;
        }
        GUI.enabled = true;

        Rect icon = theme.IconRect(header);
        float space = icon.x - header.x - 2f * theme.Padding;
        float labelWidth = space * LABEL_SHARE;
        GUI.Label(new Rect(header.x + theme.Padding, header.y, labelWidth, header.height), label, theme.Body);
        GUI.Label(new Rect(header.x + theme.Padding + labelWidth, header.y, space - labelWidth, header.height), current, theme.Value);

        _openAmount = theme.Animate(_openAmount, isOpen && hasOptions ? 1f : 0f, ARROW_SECONDS);
        theme.DrawDropdownArrow(header, _openAmount);

        if (!isOpen || !hasOptions)
        {
            _listAlpha = 0f;
            _isPressed = false;
            _isDragging = false;
            _velocity = 0f;
            return RemoteProtocol.NO_INDEX;
        }

        float itemHeight = theme.Row * ITEM_HEIGHT_IN_ROWS;
        float padding = theme.Padding;
        float contentHeight = options.Count * itemHeight;
        float minHeight = Mathf.Min(contentHeight, MIN_VISIBLE_ITEMS * itemHeight);
        float viewHeight = Mathf.Clamp(maxListHeight - theme.Gap / 2f - 2f * padding, minHeight, contentHeight);
        float maxScroll = contentHeight - viewHeight;

        if (_isScrollToSelectedNeeded)
        {
            _isScrollToSelectedNeeded = false;
            _scroll = selected * itemHeight - (viewHeight - itemHeight) / 2f;
            _velocity = 0f;
        }
        Glide(theme, itemHeight);
        _scroll = Mathf.Clamp(_scroll, 0f, maxScroll);
        if ((_scroll <= 0f && _velocity < 0f) || (_scroll >= maxScroll && _velocity > 0f))
        {
            _velocity = 0f;
        }

        _listAlpha = theme.Animate(_listAlpha, 1f, LIST_FADE_SECONDS);
        Color previous = GUI.color;
        GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * UITheme.Ease(_listAlpha));

        GUILayout.Space(theme.Gap / 2f);
        Rect card = GUILayoutUtility.GetRect(0f, viewHeight + 2f * padding, GUILayout.ExpandWidth(true),
            GUILayout.Height(viewHeight + 2f * padding));
        GUI.Box(card, GUIContent.none, theme.Card);
        Rect viewport = new Rect(card.x + padding, card.y + padding, card.width - 2f * padding, viewHeight);

        //Before the entries, so a drag never reaches them as a tap.
        HandleScrolling(viewport, itemHeight, maxScroll, theme.Row * DRAG_THRESHOLD_IN_ROWS);

        int picked = DrawItems(theme, viewport, options, selected, itemHeight);
        DrawScrollbar(theme, card, viewport, contentHeight, maxScroll);

        GUI.color = previous;
        return picked;
    }

    private int DrawItems(UITheme theme, Rect viewport, IReadOnlyList<string> options, int selected, float itemHeight)
    {
        Event current = Event.current;
        bool isOutside = current.isMouse && !viewport.Contains(current.mousePosition);
        GUI.enabled = !isOutside;

        int picked = RemoteProtocol.NO_INDEX;
        int firstVisible = Mathf.Max(0, Mathf.FloorToInt(_scroll / itemHeight));
        int lastVisible = Mathf.Min(options.Count - 1, Mathf.FloorToInt((_scroll + viewport.height) / itemHeight));

        GUI.BeginGroup(viewport);
        for (int i = firstVisible; i <= lastVisible; i++)
        {
            Rect item = new Rect(0f, i * itemHeight - _scroll, viewport.width, itemHeight);
            GUIStyle style = i == selected ? theme.SelectedListItem : theme.ListItem;
            if (GUI.Button(item, options[i], style))
            {
                picked = i;
                IsOpen = false;
            }
        }
        GUI.EndGroup();

        GUI.enabled = true;
        return picked;
    }

    private void DrawScrollbar(UITheme theme, Rect card, Rect viewport, float contentHeight, float maxScroll)
    {
        if (maxScroll <= 0f)
        {
            return;
        }

        float width = theme.Row * SCROLLBAR_WIDTH_IN_ROWS;
        float height = Mathf.Max(viewport.height * viewport.height / contentHeight, theme.Row * MIN_SCROLLBAR_IN_ROWS);
        float y = viewport.y + (viewport.height - height) * (_scroll / maxScroll);
        float x = card.xMax - (theme.Padding + width) / 2f;
        theme.DrawCircle(new Rect(x, y, width, height), SCROLLBAR);
    }

    private void HandleScrolling(Rect viewport, float itemHeight, float maxScroll, float dragThreshold)
    {
        Event current = Event.current;
        float now = Time.unscaledTime;
        switch (current.type)
        {
            case EventType.MouseDown:
                if (!viewport.Contains(current.mousePosition))
                {
                    return;
                }

                bool wasGliding = Mathf.Abs(_velocity) > 0f;
                _velocity = 0f;
                _isPressed = true;
                _isDragging = false;
                _pressY = current.mousePosition.y;
                _pressScroll = _scroll;
                _lastDragY = _pressY;
                _lastDragTime = now;
                if (wasGliding)
                {
                    current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (!_isPressed)
                {
                    return;
                }

                float distance = current.mousePosition.y - _pressY;
                if (!_isDragging && Mathf.Abs(distance) > dragThreshold)
                {
                    _isDragging = true;
                    GUIUtility.hotControl = 0;
                }
                if (_isDragging)
                {
                    _scroll = Mathf.Clamp(_pressScroll - distance, 0f, maxScroll);
                    float seconds = now - _lastDragTime;
                    if (seconds > 0f)
                    {
                        _velocity = -(current.mousePosition.y - _lastDragY) / seconds;
                        _lastDragY = current.mousePosition.y;
                        _lastDragTime = now;
                    }
                    current.Use();
                }
                break;

            case EventType.MouseUp:
                if (!_isPressed)
                {
                    return;
                }

                _isPressed = false;
                if (_isDragging)
                {
                    _isDragging = false;
                    GUIUtility.hotControl = 0;
                    if (now - _lastDragTime > MAX_PAUSE_BEFORE_GLIDE_SECONDS)
                    {
                        _velocity = 0f;
                    }
                    current.Use();
                }
                else
                {
                    _velocity = 0f;
                    if (!viewport.Contains(current.mousePosition))
                    {
                        GUIUtility.hotControl = 0;
                    }
                }
                break;

            case EventType.ScrollWheel:
                if (viewport.Contains(current.mousePosition))
                {
                    _velocity = 0f;
                    _scroll = Mathf.Clamp(_scroll + current.delta.y * itemHeight * WHEEL_ITEMS_PER_STEP, 0f, maxScroll);
                    current.Use();
                }
                break;
        }
    }

    private void Glide(UITheme theme, float itemHeight)
    {
        float step = theme.AnimationStep;
        if (_isPressed || step <= 0f)
        {
            return;
        }

        _scroll += _velocity * step;
        _velocity *= Mathf.Exp(-GLIDE_SLOWDOWN_PER_SECOND * step);
        if (Mathf.Abs(_velocity) < MIN_GLIDE_ROWS_PER_SECOND * itemHeight)
        {
            _velocity = 0f;
        }
    }
}
