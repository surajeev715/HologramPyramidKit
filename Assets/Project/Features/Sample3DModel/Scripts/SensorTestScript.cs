using UnityEngine;

public class SensorTestScript : MonoBehaviour
{
    private bool IsRed = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
     {
     }

    public void setColorToRed()
    {
        var renderer = GetComponent<Renderer>();
        renderer.material.color = Color.red;
    }

    public void setColorToGreen()
    {
        var renderer = GetComponent<Renderer>();
        renderer.material.color = Color.green;
    }

    public void switchBetween()
    {
        if (IsRed)
        {
            setColorToGreen();
            IsRed = false;
        }
        else
        {
            IsRed = true;
            setColorToRed();
        }
    }
}
