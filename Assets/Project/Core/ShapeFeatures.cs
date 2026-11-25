using System;

[Flags]
public enum ShapeFeatures
{
    None = 0,
    Clap = 1,
    Blow = 2,
    Light = 4,
    Text = 8,

    All = Clap | Blow | Light | Text
}
