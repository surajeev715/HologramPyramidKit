using System.Collections.Generic;
using UnityEngine;

public class ShapeSwitchUI
{
    private const string NO_SHAPES = "None";
    private const int MIN_VARIANTS = 2;

    private readonly ShapeSwitcher _switcher;
    private readonly ObjectControlsUI _controls = new ObjectControlsUI();
    private readonly List<string> _shapeNames = new List<string>();
    private readonly VariantNameCache _variantNames = new VariantNameCache();

    private int _selectedVariant = RemoteProtocol.NO_INDEX;

    public ShapeSwitchUI(ShapeSwitcher switcher)
    {
        _switcher = switcher;
    }

    public bool IsPicking => _controls.IsPicking;

    public void Draw(UITheme theme, IShapeCycler cycler, bool spinning, TextContext textContext, float listSpace)
    {
        IShapeCycler variants = Variants(cycler);
        IReadOnlyList<string> variantNames = _variantNames.Get(variants);
        int variantIndex = variants != null ? variants.CurrentIndex : RemoteProtocol.NO_INDEX;

        bool hasText = (ShapeFeatureDetector.Detect(_switcher.GetCurrentShape()) & ShapeFeatures.Text) != 0;
        bool? wordPerLine = hasText ? textContext.IsWordPerLine : (bool?)null;

        ObjectControlsUI.Result result = _controls.Draw(theme, ShapeNames(), _switcher.CurrentIndex, NO_SHAPES,
            variantNames, variantIndex, "", spinning, wordPerLine, listSpace);

        if (result.PickedShape != RemoteProtocol.NO_INDEX)
        {
            _switcher.Show(result.PickedShape);
        }
        if (result.PickedVariant != RemoteProtocol.NO_INDEX)
        {
            _selectedVariant = result.PickedVariant;
        }
        if (result.Spinning != spinning)
        {
            _switcher.SetSpinning(result.Spinning);
        }
        if (hasText)
        {
            textContext.IsWordPerLine = result.WordPerLine;
        }
    }

    public static float Rows()
    {
        return ObjectControlsUI.Rows(true);
    }

    public void CloseLists()
    {
        _controls.CloseLists();
    }

    public int ConsumeSelectedVariant()
    {
        int selected = _selectedVariant;
        _selectedVariant = RemoteProtocol.NO_INDEX;
        return selected;
    }

    private static IShapeCycler Variants(IShapeCycler cycler)
    {
        return cycler != null && cycler.Count >= MIN_VARIANTS ? cycler : null;
    }

    private List<string> ShapeNames()
    {
        List<GameObject> shapes = _switcher.shapes;
        int count = shapes != null ? shapes.Count : 0;
        if (_shapeNames.Count != count)
        {
            _shapeNames.Clear();
            for (int i = 0; i < count; i++)
            {
                _shapeNames.Add(shapes[i] != null ? shapes[i].name : "");
            }
        }
        return _shapeNames;
    }
}
