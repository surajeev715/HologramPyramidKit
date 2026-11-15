//What the buttons on the host do and show. A remote uses the same controls over the remote link.
public interface IHologramControls
{
    string CurrentShapeName { get; }

    bool IsSpinning { get; }

    //Empty if the current shape has no animations to switch between.
    string CurrentAnimationName { get; }

    void NextShape();

    void SetSpinning(bool value);

    void NextAnimation();
}
