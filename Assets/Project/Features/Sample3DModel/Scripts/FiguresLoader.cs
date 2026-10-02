using System.Runtime.Versioning;
using System.Collections.Generic;
using UnityEngine;

public class FiguresLoader : MonoBehaviour
{
    private const string prefabsPath = "Figures";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public List<GameObject> getPrefabs()
    {
        return new List<GameObject>(Resources.LoadAll<GameObject>(prefabsPath));
    }

    // Update is called once per frame
    void Update()
    {
        //do Nothing
    }
}
