using UnityEngine;

public class ShapeReactionDispatcher
{
    public void Dispatch(SensorContext sensorContext, TextContext textContext, GameObject gameObject, bool cycleNextPressed)
    {
        bool _cycleNextPressed = cycleNextPressed;
        bool _hasSoundOccured = sensorContext.HasClap;
        bool hasLightTrigger = sensorContext.HasLight;
        bool _hasBlowOccured = sensorContext.HasBlow;


        //Only the balloon reacts to sounds, it pops.
        BalloonPopper balloon = gameObject.GetComponentInChildren<BalloonPopper>();

        if (_hasSoundOccured && (balloon != null))
        {
            balloon.Pop();
        }

        //Only Sensortest

        SensorTestScript sensorTest = gameObject.GetComponentInChildren<SensorTestScript>();

        if ((sensorTest != null))
        {

            if (_hasSoundOccured)
            {
                sensorTest.switchBetween();
            }

            else if (hasLightTrigger)
            {
                sensorTest.setColorToYellow();
            }
        }

        //Only Candle Reacts to input

        CandleAnimationScript animationScript = gameObject.GetComponentInChildren<CandleAnimationScript>();

        if ((animationScript != null))
        {
            animationScript.Animate(hasLightTrigger, _hasBlowOccured);
        }
        else
        {
            Debug.LogWarning("[CANDLE] No CandleAnimationScript found on " + gameObject.name);
        }


        AnimationCycler cycler = gameObject.GetComponentInChildren<AnimationCycler>();
        if (cycler != null && cycler.states.Length > 1)
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
            Debug.Log($"[Text] Text Rendering script has been found going to render text : {textContext.Text}");
            script?.UpdateText(textContext.Text);
        }
    }
}
