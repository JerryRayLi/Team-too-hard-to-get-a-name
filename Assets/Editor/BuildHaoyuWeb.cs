using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class BuildHaoyuWeb
{
    [MenuItem("Build/Haoyu WebGL")]
    public static void Build()
    {
        PlayerSettings.productName = "Level Haoyu";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        string path = Environment.GetEnvironmentVariable("HAOYU_WEB_OUTPUT");
        if (string.IsNullOrEmpty(path)) path = "Builds/HaoyuWeb";
        var dependencies = AssetDatabase.GetDependencies("Assets/Scenes/Level_Haoyu.unity", true);
        Directory.CreateDirectory(path);
        File.WriteAllLines(Path.Combine(path, "scene-dependencies.txt"), dependencies);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Level_Haoyu.unity" },
            locationPathName = path, target = BuildTarget.WebGL, options = BuildOptions.None
        });
        string result = "HAOYU_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors + " bytes=" + report.summary.totalSize;
        Debug.Log(result);
        File.WriteAllText(Path.Combine(path, "build-result.txt"), result);
        if(Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
