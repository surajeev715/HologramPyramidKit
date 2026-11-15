using TMPro;
using UnityEngine;

public class TextRenderingScript : MonoBehaviour
{
    private TextMeshPro _text;
    private const string STARTING_TEXT = "WRITE SOMETHING :)";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _text = GetComponent<TextMeshPro>();
        _text.SetText(STARTING_TEXT);
    }
    
    public void UpdateText(string newText)
    {
        _text.SetText(newText);
    }


}
