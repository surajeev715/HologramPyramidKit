using UnityEngine;

public class TextInputUI
{
    public string text { get; set; }
    private string _draft = "";

    private int MAX_CHARS = 30;

    public void Draw()
    {
        _draft = GUILayout.TextField(_draft, MAX_CHARS);
        if (GUILayout.Button("Send", GUILayout.Height(80)))
        {
            if (!string.IsNullOrEmpty(_draft))
                text = _draft;
        }
    }
}
