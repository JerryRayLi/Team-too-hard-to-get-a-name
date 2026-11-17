using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildGoldPages
{
    public const string OutputDirectory = "docs/gold";
    public const string PublishedUrl = "https://jerryrayli.github.io/Team-too-hard-to-get-a-name/gold/";
    private const string TemplateDirectory = "Assets/WebGLTemplates/FullscreenEntry";

    public static string[] ValidateConfiguration()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled)
            .Select(scene => scene.path).ToArray();
        if (scenes.Length == 0 || scenes.Any(path => !File.Exists(path)))
            throw new BuildFailedException("An enabled build scene is missing. Check Build Settings / Build Profiles.");
        if (scenes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != scenes.Length)
            throw new BuildFailedException("The build scene list contains duplicate scenes.");
        if (Path.GetFileNameWithoutExtension(scenes[0]) != "MainMenu" ||
            Path.GetFileNameWithoutExtension(scenes[scenes.Length - 1]) != "WinScene")
            throw new BuildFailedException("MainMenu must be first and WinScene must be last.");
        foreach (string file in new[] { "index.html", "TemplateData/fullscreen-entry.js", "TemplateData/fullscreen-entry.css" })
            if (!File.Exists(Path.Combine(TemplateDirectory, file)))
                throw new BuildFailedException("Missing fullscreen template file: " + file);
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            throw new BuildFailedException("Install WebGL Build Support for this Unity Editor in Unity Hub.");
        return scenes;
    }

    [MenuItem("Build/Validate Gold WebGL Configuration")]
    public static void ValidateMenu()
    {
        string[] scenes = ValidateConfiguration();
        Debug.Log("GOLD_BUILD_READY: " + scenes.Length + " enabled scenes | output=" + OutputDirectory);
    }

    [MenuItem("Build/Gold WebGL for GitHub Pages")]
    public static void Build()
    {
        if (BuildPipeline.isBuildingPlayer)
            throw new BuildFailedException("Another player build is already running.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        string[] scenes = ValidateConfiguration();
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string output = Path.Combine(projectRoot, OutputDirectory);
        Directory.CreateDirectory(output);

        string previousTemplate = PlayerSettings.WebGL.template;
        int previousWidth = PlayerSettings.defaultWebScreenWidth;
        int previousHeight = PlayerSettings.defaultWebScreenHeight;
        WebGLCompressionFormat previousCompression = PlayerSettings.WebGL.compressionFormat;
        bool previousFallback = PlayerSettings.WebGL.decompressionFallback;
        bool previousHashes = PlayerSettings.WebGL.nameFilesAsHashes;
        try
        {
            PlayerSettings.WebGL.template = "PROJECT:FullscreenEntry";
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Gold WebGL build failed: " + report.summary.result +
                    ". See Console for details.");
            ValidateOutput(output);
            Debug.Log("GOLD_BUILD_SUCCEEDED: " + output + " | scenes=" + scenes.Length +
                " | Commit this folder and deploy GitHub Pages to update " + PublishedUrl);
        }
        finally
        {
            // Building Gold should not overwrite the team's normal player settings.
            PlayerSettings.WebGL.template = previousTemplate;
            PlayerSettings.defaultWebScreenWidth = previousWidth;
            PlayerSettings.defaultWebScreenHeight = previousHeight;
            PlayerSettings.WebGL.compressionFormat = previousCompression;
            PlayerSettings.WebGL.decompressionFallback = previousFallback;
            PlayerSettings.WebGL.nameFilesAsHashes = previousHashes;
        }
    }

    private static void ValidateOutput(string output)
    {
        foreach (string file in new[] { "index.html", "TemplateData/fullscreen-entry.js", "TemplateData/fullscreen-entry.css" })
            if (!File.Exists(Path.Combine(output, file)))
                throw new BuildFailedException("Gold build output is incomplete: " + file);
        string build = Path.Combine(output, "Build");
        if (!Directory.Exists(build) || !Directory.GetFiles(build, "*.loader.js").Any() ||
            !Directory.GetFiles(build, "*.wasm*").Any() || !Directory.GetFiles(build, "*.data*").Any() ||
            !Directory.GetFiles(build, "*.framework.js*").Any())
            throw new BuildFailedException("Gold build output is missing Unity runtime files.");
    }

}
