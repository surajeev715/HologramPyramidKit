using UnityEngine;

public class ShapeReactionDispatcher
{
    public void Dispatch(SensorContext sensorContext, TextContext textContext, GameObject gameObject, bool cycleNextPressed)
    {
        bool _cycleNextPressed = cycleNextPressed;
        bool hasLightTrigger = sensorContext.HasLight;
        bool _hasBlowOccured = sensorContext.HasBlow;


        //Only Candle Reacts to input

        CandleAnimationScript animationScript = gameObject.GetComponentInChildren<CandleAnimationScript>();

        if ((animationScript != null))
        {
            animationScript.Animate(hasLightTrigger, _hasBlowOccured);
        }


        IShapeCycler cycler = gameObject.GetComponentInChildren<IShapeCycler>();
        if (cycler != null && cycler.Count > 1)
        {

            if (_cycleNextPressed)
            {
                cycler.Next();
            }
        }

        //for Text Rendering
        TextRenderingScript script = gameObject.GetComponentInChildren<TextRenderingScript>();
        if(script != null)
        {
            script?.UpdateText(textContext.Text);
        }
    }
}
