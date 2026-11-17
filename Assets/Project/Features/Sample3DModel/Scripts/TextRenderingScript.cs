using TMPro;
using UnityEngine;

public class TextRenderingScript : MonoBehaviour
{
    private TextMeshPro _text;
    private const string STARTING_TEXT = "WRITE SOMETHING I got a script :)";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _text = GetComponentInChildren<TextMeshPro>();
        _text.SetText(STARTING_TEXT);
    }
    
    public void UpdateText(string newText)
    {
        if(_text == null)
            _text = GetComponentInChildren<TextMeshPro>();
        _text.SetText(newText);
        Debug.Log($"Update Text has been called new value: {newText}");
    }


}
