using UnityEngine;

public class TextInputUI
{
    public string text { get; set; }

    private int MAX_CHARS = 30;

    public void Draw()
    {
        text = GUILayout.TextField(text, MAX_CHARS);
    }
}
