using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class WebGLBuildScript
{
    private const string OutputPath = "RenderSite";

    public static void Build()
    {
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };
        RenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/URPAsset.asset");
        if (urp != null)
        {
            GraphicsSettings.defaultRenderPipeline = urp;
            int currentQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(currentQuality, false);
            EditorUtility.SetDirty(urp);
            AssetDatabase.SaveAssets();
        }

        if (Directory.Exists(OutputPath))
            Directory.Delete(OutputPath, true);
        Directory.CreateDirectory(OutputPath);

        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
            throw new System.Exception($"WebGL build failed: {summary.result}");

        Debug.Log($"WebGL build succeeded: {summary.totalSize} bytes -> {OutputPath}");
    }
}
