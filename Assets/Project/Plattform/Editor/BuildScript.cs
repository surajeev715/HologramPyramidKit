using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    private const string SCENE_PATH = "Assets/Project/Scenes/HologramScene.unity";

    private const string WINDOWS_BUILD_PATH = "Builds/Windows/HologramApp.exe";

    [MenuItem("Builds/Build Windows")]
    public static void BuildWindows()
    {
        string[] Scenes = new string[] { SCENE_PATH };
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = WINDOWS_BUILD_PATH,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
        Debug.Log("[BuildScript] Windows Build abgeschlossen!");
    }

    [MenuItem("Builds/Build Apk")]
    public static void BuildApk()
    {

    }

    [MenuItem("Builds/Build Web")]
    public static void BuildWeb()
    {

    }
}