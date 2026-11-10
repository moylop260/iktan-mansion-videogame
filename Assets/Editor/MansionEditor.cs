using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Prepara el proyecto (URP, materiales base, escena, ajustes) y compila la app de macOS.</summary>
public static class MansionEditor
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string RpPath = "Assets/Settings/URP.asset";

    [InitializeOnLoadMethod]
    static void Auto()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) Setup();
            SetupRendering();
            ApplySettings();
        };
    }

    [MenuItem("Mansion/Preparar proyecto")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        SetupRendering();
        ApplySettings();
        AssetDatabase.SaveAssets();
    }

    static void SetBool(SerializedObject so, string name, bool v)
    {
        var p = so.FindProperty(name); if (p != null) p.boolValue = v;
    }
    static void SetInt(SerializedObject so, string name, int v)
    {
        var p = so.FindProperty(name); if (p != null) p.intValue = v;
    }
    static void SetFloat(SerializedObject so, string name, float v)
    {
        var p = so.FindProperty(name); if (p != null) p.floatValue = v;
    }

    /// <summary>Crea el pipeline URP y los materiales base (en Resources para que entren al build).</summary>
    public static void SetupRendering()
    {
        Directory.CreateDirectory("Assets/Settings");
        Directory.CreateDirectory("Assets/Resources");

        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(RpPath);
        if (asset == null)
        {
            var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
            rd.renderingMode = RenderingMode.ForwardPlus;
            AssetDatabase.CreateAsset(rd, "Assets/Settings/URP-Renderer.asset");
            asset = UniversalRenderPipelineAsset.Create(rd);
            AssetDatabase.CreateAsset(asset, RpPath);

            var so = new SerializedObject(asset);
            SetBool(so, "m_AdditionalLightShadowsSupported", true);
            SetInt(so, "m_AdditionalLightsShadowmapResolution", 1024);
            SetBool(so, "m_SoftShadowsSupported", true);
            SetFloat(so, "m_ShadowDistance", 35f);
            SetBool(so, "m_SupportsHDR", true);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
        GraphicsSettings.defaultRenderPipeline = asset;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        QualitySettings.SetQualityLevel(current, false);

        MakeMat("Assets/Resources/BaseLit.mat", false);
        MakeMat("Assets/Resources/BaseLitEmit.mat", true);
        AssetDatabase.SaveAssets();
    }

    static void MakeMat(string path, bool emission)
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        var m = new Material(sh);
        if (emission)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.white);
        }
        AssetDatabase.CreateAsset(m, path);
    }

    static void ApplySettings()
    {
        PlayerSettings.companyName = "IkLpz";
        PlayerSettings.productName = "La Mansion";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.iktan.mansion");
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
    }

    [MenuItem("Mansion/Compilar para macOS")]
    public static void BuildMac()
    {
        if (!File.Exists(ScenePath)) Setup();
        SetupRendering();
        ApplySettings();
        Directory.CreateDirectory("Builds");
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/Mansion.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        bool ok = report.summary.result == BuildResult.Succeeded;
        Debug.Log("Compilación " + (ok ? "OK" : "FALLÓ") + ": " + report.summary.outputPath);
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }
}
