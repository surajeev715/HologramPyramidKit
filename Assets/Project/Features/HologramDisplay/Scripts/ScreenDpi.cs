using UnityEngine;

//Physical screen density shared by everything that is sized in centimeters, so all of it uses the same calibration.
public static class ScreenDpi
{
    //Used when the platform does not report a DPI (Screen.dpi returns 0).
    const float fallbackDpi = 160f;

    //Overrides the detected DPI when greater than 0. Set by Hologram from its inspector calibration value.
    public static float overrideDpi = 0f;

    static float detectedDpi = -1f;

    public static float Dpi
    {
        get
        {
            if (overrideDpi > 0f)
            {
                return overrideDpi;
            }

            if (detectedDpi < 0f)
            {
                detectedDpi = DetectDpi();
            }

            return detectedDpi;
        }
    }

    public static float PixelsPerCm
    {
        get { return Dpi / 2.54f; }
    }

    static float DetectDpi()
    {
        float dpi = Screen.dpi;

#if UNITY_ANDROID && !UNITY_EDITOR
        //Screen.dpi reports Android's density bucket (e.g. 320, 480), which can be off by 20% or more from the real panel.
        //xdpi and ydpi are the physical values, but some devices report nonsense, so they are only used when close to the bucket.
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject resources = activity.Call<AndroidJavaObject>("getResources"))
            using (AndroidJavaObject metrics = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
            {
                float physical = (metrics.Get<float>("xdpi") + metrics.Get<float>("ydpi")) / 2f;
                float bucket = metrics.Get<int>("densityDpi");

                if (physical > bucket * 0.6f && physical < bucket * 1.6f)
                {
                    dpi = physical;
                }
                else if (bucket > 0f)
                {
                    dpi = bucket;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ScreenDpi] Could not read the Android display metrics: " + e.Message);
        }
#endif

        return dpi > 0f ? dpi : fallbackDpi;
    }
}
