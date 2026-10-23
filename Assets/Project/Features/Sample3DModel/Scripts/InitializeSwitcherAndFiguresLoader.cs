using UnityEngine;
using System.Collections.Generic;
public class InitializeSwitcherAndFiguresLoader : MonoBehaviour

{
    private FiguresLoader loader;
    private ShapeSwitcher switcher;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loader = GetComponent<FiguresLoader>();
        switcher = GetComponent<ShapeSwitcher>();


        List<GameObject> prefabs = loader.getPrefabs();
        switcher.SetShapes(AddPrefabsToSceneAndGetItsInstances(prefabs));
        switcher.StartSwitcher();
    }

    private List<GameObject> AddPrefabsToSceneAndGetItsInstances(List<GameObject> prefabs)
    {
        List<GameObject> sceneInstances = new List<GameObject>();
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null) continue;

            // 1. Erstelle die echte Kopie in der Szene
            GameObject instance = Instantiate(prefab);

            // 2. Name aufräumen (entfernt das "(Clone)" am Ende des Namens)
            instance.name = prefab.name;

            // 3. Sofort unsichtbar machen!
            instance.SetActive(false);

            // 4. In die Szene-Liste speichern
            sceneInstances.Add(instance);
        }
        return sceneInstances;
    }
    // Update is called once per frame
    void Update()
    {
        switcher.RunSensor();
    }
}
