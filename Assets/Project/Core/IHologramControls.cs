using System.Collections.Generic;

//What the controls on the host do and show. A remote uses the same controls over the remote link.
public interface IHologramControls
{
    string CurrentShapeName { get; }

    IReadOnlyList<string> ShapeNames { get; }
    int CurrentShapeIndex { get; }

    ShapeFeatures CurrentFeatures { get; }

    bool IsSpinning { get; }

    IReadOnlyList<string> VariantNames { get; }
    int CurrentVariantIndex { get; }
    string CurrentVariantName { get; }

    bool IsWordPerLine { get; }

    void ShowShape(int index);

    void SetWordPerLine(bool value);

    void SetSpinning(bool value);

    void SelectVariant(int index);
}
