using System.Collections.Generic;
using UnityEngine;

//A remote shows no hologram: its screen only has the remote's buttons (RemoteDeviceUI) on black.
public static class RemoteScreen
{
    private const int BACKGROUND_CAMERA = 0;

    public static void HideHologram(List<GameObject> shapes)
    {
        foreach (GameObject shape in shapes)
        {
            if (shape != null)
            {
                shape.SetActive(false);
            }
        }

        Hologram hologram = Object.FindAnyObjectByType<Hologram>();
        if (hologram == null)
        {
            return;
        }

        //Stops the four views and the center square from being drawn.
        hologram.enabled = false;

        //The first camera only clears the screen and stays on, the view cameras have nothing to render anymore.
        for (int i = 0; i < hologram.cameras.Length; i++)
        {
            if (hologram.cameras[i] == null)
            {
                continue;
            }

            if (i == BACKGROUND_CAMERA)
            {
                hologram.cameras[i].backgroundColor = Color.black;
            }
            else
            {
                hologram.cameras[i].enabled = false;
            }
        }
    }
}
