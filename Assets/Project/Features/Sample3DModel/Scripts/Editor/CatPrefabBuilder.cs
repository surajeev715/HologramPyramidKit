using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using System.Linq;

//Builds a hologram figure prefab from the animated cat model. FiguresLoader picks it up from Resources/Figures like the other shapes.
public static class CatPrefabBuilder
{
    const string modelPath = "Assets/Project/Models/siamese_cat_animated.fbx";
    const string controllerPath = "Assets/Project/Models/Cat.controller";
    const string materialPath = "Assets/Project/Models/Cat Gradient.mat";
    const string shaderName = "Hologram/Hue Gradient";
    const string prefabFolder = "Assets/Project/Features/Sample3DModel/Resources/Figures";
    const string prefabName = "Cat";

    //The figures have to fit the space the Hologram cameras are set up for: 1 unit high and 0.71 units from the vertical axis while spinning.
    const float maxHalfHeight = 0.5f;
    const float maxRadius = 0.71f;

    [MenuItem("Tools/Hologram/Build Cat Prefab")]
    public static void BuildCatPrefab()
    {
        ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);

        if (importer == null)
        {
            Debug.LogError("[CatPrefabBuilder] Model not found at " + modelPath);
            return;
        }

        MakeClipsLoop(importer);

        AnimationClip[] clips = AssetDatabase.LoadAllAssetRepresentationsAtPath(modelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToArray();

        AnimatorController controller = CreateController(clips);

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        GameObject root = new GameObject(prefabName);
        root.AddComponent<CubeAnimation>();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        instance.transform.SetParent(root.transform, false);

        Animator animator = instance.GetComponent<Animator>();

        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        //Skinned meshes are often culled wrongly by the off-center hologram cameras, so the cat always animates.
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skinned.updateWhenOffscreen = true;
        }

        AnimationCycler cycler = instance.AddComponent<AnimationCycler>();
        cycler.states = clips.Select(clip => clip.name).ToArray();

        AssignMaterial(instance, CreateMaterial());

        //The hologram shows the figures upside down, like the pyramid that is built pointing downwards, so the cat is flipped too.
        instance.transform.localRotation = Quaternion.Euler(180f, 0f, 0f) * instance.transform.localRotation;

        FitToHologramArea(root.transform, instance.transform);

        Directory.CreateDirectory(prefabFolder);
        PrefabUtility.SaveAsPrefabAsset(root, prefabFolder + "/" + prefabName + ".prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();

        Debug.Log("[CatPrefabBuilder] Built " + prefabName + " prefab with " + clips.Length + " animation(s): " + string.Join(", ", cycler.states));
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

    static void AssignMaterial(GameObject model, Material material)
    {
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
        }
    }

    static void MakeClipsLoop(ModelImporter importer)
    {
        ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;

        if (clipAnimations.All(clip => clip.loopTime))
        {
            return;
        }

        foreach (ModelImporterClipAnimation clip in clipAnimations)
        {
            clip.loopTime = true;
        }

        importer.clipAnimations = clipAnimations;
        importer.SaveAndReimport();
    }

    static AnimatorController CreateController(AnimationClip[] clips)
    {
        //Recreated from scratch so it always matches the clips in the model.
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        foreach (AnimationClip clip in clips)
        {
            AnimatorState state = stateMachine.AddState(clip.name);
            state.motion = clip;
        }

        return controller;
    }

    static void FitToHologramArea(Transform root, Transform model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("[CatPrefabBuilder] The model has no renderers, it is not scaled.");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        //The figure spins around its vertical axis, so its horizontal size is measured from the center to the farthest corner.
        float halfHeight = bounds.extents.y;
        float radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
        float scale = Mathf.Min(maxHalfHeight / halfHeight, maxRadius / radius);

        model.localScale *= scale;
        model.localPosition = -(bounds.center - root.position) * scale;
    }
}
