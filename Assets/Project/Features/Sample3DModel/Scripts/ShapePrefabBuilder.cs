using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//Generates the pyramid and ball meshes, saves them as assets and builds a prefab for each, like the cube prefab.
public static class ShapePrefabBuilder
{
    const string meshFolder = "Assets/Example/Meshes";
    const string prefabFolder = "Assets/Example/Prefab";
    const string materialPath = "Assets/Example/Materials/Vertex Color.mat";
    const string shaderName = "Hologram/Vertex Color";

    //Size of the generated shapes, matching the 1 unit cube.
    const float shapeSize = 1f;

    [MenuItem("Tools/Hologram/Rebuild Shape Prefabs")]
    public static void RebuildPrefabs()
    {
        Material material = CreateMaterial();
        CreatePrefab("Pyramid", PyramidMesh(shapeSize), material);
        CreatePrefab("Ball", BallMesh(shapeSize / 2f, 24, 48), material);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Hologram/Add Shape Prefabs To Scene")]
    public static void AddPrefabsToScene()
    {
        RebuildPrefabs();

        ShapeSwitcher switcher = Object.FindAnyObjectByType<ShapeSwitcher>(FindObjectsInactive.Include);

        if (switcher == null)
        {
            Debug.LogError("[ShapePrefabBuilder] No ShapeSwitcher found in the open scene.");
            return;
        }

        foreach (string shapeName in new[] { "Pyramid", "Ball" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFolder + "/" + shapeName + ".prefab");

            //Skip prefabs that already have an instance in the switcher's list.
            if (switcher.shapes.Any(shape => shape != null && PrefabUtility.GetCorrespondingObjectFromSource(shape) == prefab))
            {
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, switcher.gameObject.scene);
            instance.SetActive(false);
            Undo.RegisterCreatedObjectUndo(instance, "Add " + shapeName);
            Undo.RecordObject(switcher, "Add " + shapeName);
            switcher.shapes.Add(instance);
        }

        EditorUtility.SetDirty(switcher);
        EditorSceneManager.MarkSceneDirty(switcher.gameObject.scene);
    }


    //Entry point for batch mode: adds the prefabs to the Example scene and saves it.
    public static void BuildForExampleScene()
    {
        EditorSceneManager.OpenScene("Assets/Example/Scene/Example.unity");
        AddPrefabsToScene();
        EditorSceneManager.SaveOpenScenes();
    }

    static Material CreateMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

        if (material == null)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, materialPath);
        }

        return material;
    }

    static void CreatePrefab(string shapeName, Mesh mesh, Material material)
    {
        Directory.CreateDirectory(meshFolder);
        string meshPath = meshFolder + "/" + shapeName + ".asset";

        //Overwrite an existing mesh in place so prefabs referencing it keep working.
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            mesh = existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        GameObject shape = new GameObject(shapeName);
        shape.AddComponent<MeshFilter>().sharedMesh = mesh;
        shape.AddComponent<MeshRenderer>().sharedMaterial = material;
        shape.AddComponent<CubeAnimation>();

        PrefabUtility.SaveAsPrefabAsset(shape, prefabFolder + "/" + shapeName + ".prefab");
        Object.DestroyImmediate(shape);
    }

    static Mesh PyramidMesh(float size)
    {
        //Four-sided pyramid pointing downwards, with the same bounds as the cube.
        float h = size / 2f;
        Vector3 apex = new Vector3(0, -h, 0);
        Vector3[] corners =
        {
            new Vector3(-h, h, -h),
            new Vector3(h, h, -h),
            new Vector3(h, h, h),
            new Vector3(-h, h, h)
        };
        Color[] sideColors =
        {
            new Color(1f, 0.2f, 0.2f),
            new Color(0.12f, 0.55f, 0.15f),
            new Color(0.45f, 0.12f, 0.6f),
            new Color(0.65f, 0.68f, 0.13f)
        };
        Color baseColor = new Color(1f, 0.59f, 0f);

        //Every face has its own vertices so it gets a flat normal and a single color.
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();

        for (int i = 0; i < 4; i++)
        {
            vertices.Add(apex);
            vertices.Add(corners[i]);
            vertices.Add(corners[(i + 1) % 4]);
            colors.Add(sideColors[i]);
            colors.Add(sideColors[i]);
            colors.Add(sideColors[i]);
        }

        //The base is on top and faces upwards.
        vertices.Add(corners[0]);
        vertices.Add(corners[2]);
        vertices.Add(corners[1]);
        vertices.Add(corners[0]);
        vertices.Add(corners[3]);
        vertices.Add(corners[2]);

        for (int i = 0; i < 6; i++)
        {
            colors.Add(baseColor);
        }

        int[] triangles = new int[vertices.Count];

        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = i;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Pyramid";
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BallMesh(float radius, int latitudes, int longitudes)
    {
        //UV sphere whose color runs through all hues around its equator, so its rotation stays visible without edges.
        int rowLength = longitudes + 1;
        Vector3[] vertices = new Vector3[(latitudes + 1) * rowLength];
        Vector3[] normals = new Vector3[vertices.Length];
        Color[] colors = new Color[vertices.Length];

        for (int lat = 0; lat <= latitudes; lat++)
        {
            float theta = Mathf.PI * lat / latitudes;

            for (int lon = 0; lon <= longitudes; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudes;
                Vector3 normal = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));

                int index = lat * rowLength + lon;
                vertices[index] = normal * radius;
                normals[index] = normal;
                colors[index] = Color.HSVToRGB((float)lon / longitudes, 0.85f, 1f);
            }
        }

        int[] triangles = new int[latitudes * longitudes * 6];
        int t = 0;

        for (int lat = 0; lat < latitudes; lat++)
        {
            for (int lon = 0; lon < longitudes; lon++)
            {
                int topLeft = lat * rowLength + lon;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + rowLength;
                int bottomRight = bottomLeft + 1;

                triangles[t++] = topLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomRight;
                triangles[t++] = topLeft;
                triangles[t++] = bottomRight;
                triangles[t++] = bottomLeft;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Ball";
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
