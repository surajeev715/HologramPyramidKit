using UnityEngine;

//Lets the remote link press the host's buttons: forwards to the ShapeSwitcher and the current shape.
public class ShapeSwitcherControls : IHologramControls
{
    private const int MIN_STATES_TO_SWITCH = 2;

    private readonly ShapeSwitcher _switcher;

    public ShapeSwitcherControls(ShapeSwitcher switcher)
    {
        _switcher = switcher;
    }

    public string CurrentShapeName
    {
        get
        {
            GameObject shape = _switcher.GetCurrentShape();
            return shape != null ? shape.name : "";
        }
    }

    public bool IsSpinning => _switcher.spinning;

    public string CurrentAnimationName
    {
        get
        {
            IShapeCycler cycler = FindCycler();
            return cycler != null ? cycler.CurrentName : "";
        }
    }

    public void NextShape()
    {
        _switcher.NextShape();
    }

    public void SetSpinning(bool value)
    {
        _switcher.SetSpinning(value);
    }

    public void NextAnimation()
    {
        IShapeCycler cycler = FindCycler();
        if (cycler != null)
        {
            cycler.Next();
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
