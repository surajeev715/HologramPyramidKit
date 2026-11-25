using UnityEngine;

public static class ShapeFeatureDetector
{
    public static ShapeFeatures Detect(GameObject shape)
    {
        if (shape == null)
        {
            return ShapeFeatures.None;
        }

        ShapeFeatures features = ShapeFeatures.None;
        if (shape.GetComponentInChildren<BalloonPopper>() != null)
        {
            features |= ShapeFeatures.Clap;
        }
        if (shape.GetComponentInChildren<CandleAnimationScript>() != null)
        {
            features |= ShapeFeatures.Light | ShapeFeatures.Blow;
        }
        if (shape.GetComponentInChildren<TextRenderingScript>() != null)
        {
            features |= ShapeFeatures.Text;
        }
        return features;
    }
}
