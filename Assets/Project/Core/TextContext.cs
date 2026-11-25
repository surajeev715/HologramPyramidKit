using System;
using System.Collections.Generic;
using System.Text;

public class TextContext
{
    public string Text { get; set; }

    public bool IsWordPerLine { get; set; }

    public TextContext(string text)
    {
        Text = text;
    }
}
