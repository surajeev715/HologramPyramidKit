using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    private const string SCENE_PATH = "Assets/Project/Scenes/HologramScene.unity";

    private const string WINDOWS_BUILD_PATH = "Builds/Windows/HologramApp.exe";

    private const string APK_BUILD_PATH = "Builds/Android/HologramApp.apk";

    private const string WEB_BUILD_PATH = "Builds/Web";

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
        SwitchTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

        BuildPipeline.BuildPlayer(options);
        Debug.Log("[BuildScript] Windows Build abgeschlossen!");
    }

    [MenuItem("Builds/Build Apk")]
    public static void BuildApk()
    {

        EditorPrefs.SetBool("JdkUseEmbedded", true);
        EditorPrefs.SetBool("SdkUseEmbedded", true);
        EditorPrefs.SetBool("NdkUseEmbedded", true);
        SwitchTarget(BuildTargetGroup.Android, BuildTarget.Android);

        string[] Scenes = new string[] { SCENE_PATH };
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = APK_BUILD_PATH,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(options);
        Debug.Log("[BuildScript] Apk Build abgeschlossen!");

    }

    [MenuItem("Builds/Build Web")]
    public static void BuildWeb()
    {
        SwitchTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new string[] { SCENE_PATH },

            locationPathName = WEB_BUILD_PATH,

            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        
        BuildPipeline.BuildPlayer(options);
    }

    private static void SwitchTarget(BuildTargetGroup group, BuildTarget target)
    {
        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
        }
    }
}