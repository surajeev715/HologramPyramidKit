using UnityEngine;

public class TextInputUI
{
    private const int MAX_CHARS = 30;
    private const float SEND_WIDTH_IN_ROWS = 2.2f;

    public string text { get; set; }
    private string _draft = "";

    public void Draw(UITheme theme)
    {
        GUILayoutOption height = GUILayout.Height(theme.Row);

        GUILayout.BeginHorizontal();
        _draft = GUILayout.TextField(_draft, MAX_CHARS, theme.TextField, height, GUILayout.ExpandWidth(true));
        GUILayout.Space(theme.Gap);
        GUI.enabled = !string.IsNullOrEmpty(_draft);
        if (GUILayout.Button("Send", theme.AccentButton, height, GUILayout.Width(theme.Row * SEND_WIDTH_IN_ROWS)))
        {
            text = _draft;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }
}
