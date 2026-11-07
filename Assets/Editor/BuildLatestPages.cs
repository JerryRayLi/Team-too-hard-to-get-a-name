using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildLatestPages
{
    public const string OutputDirectory = "docs/game-lastest-update";

    // Exposed for a pre-build check without starting a WebGL compilation.
    public static string[] ValidateConfiguration()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(item => item.enabled)
            .Select(item => item.path).ToArray();
        if (scenes.Length == 0 || scenes.Any(item => !File.Exists(item)))
            throw new BuildFailedException("An enabled build scene is missing.");
        string[] names = scenes.Select(Path.GetFileNameWithoutExtension).ToArray();
        RequireNext(names, "level_j2", "level_j3");
        RequireNext(names, "Level_Running_Door_1", "Level_Running_Door_2");
        if (names[0] != "MainMenu" || names[names.Length - 1] != "WinScene")
            throw new BuildFailedException("MainMenu must be first and WinScene must be last.");
        if (!File.Exists("Assets/WebGLTemplates/FullscreenEntry/index.html"))
            throw new BuildFailedException("The FullscreenEntry WebGL template is missing.");
        return scenes;
    }

    [MenuItem("Build/Latest WebGL for GitHub Pages")]
    public static void Build()
    {
        string[] scenes = ValidateConfiguration();
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new BuildFailedException("Install WebGL Build Support for this Unity Editor first.");

        PlayerSettings.WebGL.template = "PROJECT:FullscreenEntry";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        Directory.CreateDirectory(OutputDirectory);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDirectory,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Latest Pages build failed: " + report.summary.result);
        Debug.Log("PAGES_BUILD_SUCCEEDED: " + Path.GetFullPath(OutputDirectory) +
            " | scenes=" + scenes.Length + " | publish this folder on gh-pages under docs.");
    }

    private static void RequireNext(string[] names, string first, string second)
    {
        int index = Array.IndexOf(names, first);
        if (index < 0 || index + 1 >= names.Length || names[index + 1] != second)
            throw new BuildFailedException(second + " must immediately follow " + first + ".");
    }
}
