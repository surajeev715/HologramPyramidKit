using System.Collections.Generic;
using UnityEngine;

//Lets the remote link use the host's controls: forwards to the ShapeSwitcher and the current shape.
public class ShapeSwitcherControls : IHologramControls
{
    private const int MIN_STATES_TO_SWITCH = 2;

    private readonly ShapeSwitcher _switcher;
    private readonly TextContext _textContext;

    private readonly List<string> _shapeNames = new List<string>();
    private readonly VariantNameCache _variantNames = new VariantNameCache();

    public ShapeSwitcherControls(ShapeSwitcher switcher, TextContext textContext)
    {
        _switcher = switcher;
        _textContext = textContext;
    }

    public string CurrentShapeName
    {
        get
        {
            GameObject shape = _switcher.GetCurrentShape();
            return shape != null ? shape.name : "";
        }
    }

    public IReadOnlyList<string> ShapeNames
    {
        get
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

    public int CurrentShapeIndex => _switcher.CurrentIndex;

    public ShapeFeatures CurrentFeatures => ShapeFeatureDetector.Detect(_switcher.GetCurrentShape());

    public bool IsSpinning => _switcher.spinning;

    public IReadOnlyList<string> VariantNames => _variantNames.Get(FindCycler());

    public int CurrentVariantIndex
    {
        get
        {
            IShapeCycler cycler = FindCycler();
            return cycler != null ? cycler.CurrentIndex : RemoteProtocol.NO_INDEX;
        }
    }

    public string CurrentVariantName
    {
        get
        {
            IShapeCycler cycler = FindCycler();
            return cycler != null ? cycler.CurrentName : "";
        }
    }

    public bool IsWordPerLine => _textContext.IsWordPerLine;

    public void SetWordPerLine(bool value)
    {
        _textContext.IsWordPerLine = value;
    }

    public void ShowShape(int index)
    {
        List<GameObject> shapes = _switcher.shapes;
        if (shapes != null && index >= 0 && index < shapes.Count)
        {
            _switcher.Show(index);
        }
    }

    public void SetSpinning(bool value)
    {
        _switcher.SetSpinning(value);
    }

    public void SelectVariant(int index)
    {
        IShapeCycler cycler = FindCycler();
        if (cycler != null)
        {
            cycler.Select(index);
        }
    }

    //Only shapes with several versions (e.g. the balloons) have something to switch.
    private IShapeCycler FindCycler()
    {
        GameObject shape = _switcher.GetCurrentShape();
        if (shape == null)
        {
            return null;
        }

        IShapeCycler cycler = shape.GetComponentInChildren<IShapeCycler>();
        return cycler != null && cycler.Count >= MIN_STATES_TO_SWITCH ? cycler : null;
    }
}
