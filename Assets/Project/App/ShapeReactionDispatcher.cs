using UnityEngine;

public class ShapeReactionDispatcher
{
    public void Dispatch(SensorContext sensorContext, TextContext textContext, GameObject gameObject, int selectedVariant)
    {
        bool hasLightTrigger = sensorContext.HasLight;
        bool _hasBlowOccured = sensorContext.HasBlow;
        bool hasClap = sensorContext.HasClap;


        //Only the balloons react to claps, the shown balloon pops.
        BalloonPopper popper = gameObject.GetComponentInChildren<BalloonPopper>();

        if (popper != null)
        {
            popper.React(hasClap);
        }

        //Only Candle Reacts to input

        CandleAnimationScript animationScript = gameObject.GetComponentInChildren<CandleAnimationScript>();

        if ((animationScript != null))
        {
            animationScript.Animate(hasLightTrigger, _hasBlowOccured);
        }


        IShapeCycler cycler = gameObject.GetComponentInChildren<IShapeCycler>();
        if (cycler != null && cycler.Count > 1 && selectedVariant != RemoteProtocol.NO_INDEX)
        {
            cycler.Select(selectedVariant);
        }

        //for Text Rendering
        TextRenderingScript script = gameObject.GetComponentInChildren<TextRenderingScript>();
        if(script != null)
        {
            script.UpdateText(textContext.Text, textContext.IsWordPerLine);
        }
    }
}
