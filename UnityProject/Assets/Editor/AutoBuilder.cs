using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AutoBuilder
{
    public static void BuildAndroid()
    {
        string[] scenes = GetEnabledScenePaths();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in Editor Build Settings.");
        }

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string buildPath = Path.Combine(projectRoot, "Builds", "ColorHunt.apk");

        Directory.CreateDirectory(Path.GetDirectoryName(buildPath));

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new Exception($"Android build failed: {report.summary.result}");
        }
    }

    private static string[] GetEnabledScenePaths()
    {
        var scenes = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        return scenes.ToArray();
    }
}
