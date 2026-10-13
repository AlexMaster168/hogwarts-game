using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Настройка проекта и сборка. Меню Hogwarts/... или batchmode: -executeMethod ProjectSetup.Setup / ProjectSetup.Build
public static class ProjectSetup
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Hogwarts/Setup Project")]
    public static void Setup()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.productName = "Hogwarts Mini";
        PlayerSettings.companyName = "Lyokha";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;

        Directory.CreateDirectory("Assets/Resources");
        MakeMat("HM_Tri", "HM/Triplanar");
        MakeMat("HM_Add", "HM/Additive");
        MakeMat("HM_Fade", "HM/Fade");
        MakeMat("HM_Sky", "Skybox/Procedural");
        MakeMat("HM_Text", "HM/Text");
        MakePostRes();

        if (!File.Exists(ScenePath))
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Game").AddComponent<GameManager>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ModelSetup.Run();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("HM_SETUP_OK");
    }

    static void MakeMat(string name, string shader)
    {
        string path = $"Assets/Resources/{name}.mat";
        var sh = Shader.Find(shader);
        if (sh == null) { Debug.LogError("Shader not found: " + shader); return; }
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.shader = sh; EditorUtility.SetDirty(existing); return; }
        AssetDatabase.CreateAsset(new Material(sh), path);
    }

    // Ассет со ссылкой на PostProcessResources пакета: без него PostProcessLayer в рантайме не инициализировать.
    static void MakePostRes()
    {
        const string path = "Assets/Resources/HM_PostRes.asset";
        var res = AssetDatabase.FindAssets("t:PostProcessResources")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>)
            .FirstOrDefault(r => r != null);
        if (res == null)
            res = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>(
                "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
        if (res == null) { Debug.LogError("PostProcessResources not found"); return; }
        var holder = AssetDatabase.LoadAssetAtPath<PostResHolder>(path);
        if (holder == null)
        {
            holder = ScriptableObject.CreateInstance<PostResHolder>();
            AssetDatabase.CreateAsset(holder, path);
        }
        holder.resources = res;
        EditorUtility.SetDirty(holder);
    }

    [MenuItem("Hogwarts/Build Windows")]
    public static void Build()
    {
        Setup();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Build/HogwartsMini.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        Debug.Log($"HM_BUILD_RESULT {report.summary.result} errors={report.summary.totalErrors} size={report.summary.totalSize}");
        if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
