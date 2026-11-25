using UnityEngine;

//Shows one balloon at a time and switches to another one with Select(), like the lion switches between its animations.
public class BalloonCycler : MonoBehaviour, IShapeCycler
{
    [Tooltip("The balloons to switch between, in order. Only one of them is shown at a time.")]
    public GameObject[] balloons = new GameObject[0];

    private int current = 0;

    void OnEnable()
    {
        //Start with the first balloon whenever the figure is shown again.
        current = 0;
        ShowCurrent();
    }

    public int Count
    {
        get { return balloons.Length; }
    }

    public int CurrentIndex
    {
        get { return current; }
    }

    public string CurrentName
    {
        get { return balloons.Length > 0 ? balloons[current].name : ""; }
    }

    public string NameAt(int index)
    {
        return index >= 0 && index < balloons.Length ? balloons[index].name : "";
    }

    public void Select(int index)
    {
        if (index < 0 || index >= balloons.Length || index == current)
        {
            return;
        }

        current = index;
        ShowCurrent();
    }

    void ShowCurrent()
    {
        for (int i = 0; i < balloons.Length; i++)
        {
            balloons[i].SetActive(i == current);
        }
    }
}
