// SPDX-AI-Disclosure: ai-generated
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorScripts
{
    public static class WebGlBuildScript
    {
        [MenuItem("Tools/Build WebGL")]
        public static void BuildWebGL()
        {
            string buildPath = Path.Combine(Directory.GetCurrentDirectory(), "Builds/WebGL");
            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/MainGame.unity" },
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            Debug.Log($"[WebGlBuildScript] Starting WebGL build to: {buildPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGlBuildScript] WebGL build SUCCEEDED: {summary.totalSize} bytes, {summary.totalTime.TotalSeconds:F1}s");
            }
            else
            {
                Debug.LogError($"[WebGlBuildScript] WebGL build FAILED with result: {summary.result}, errors: {summary.totalErrors}");
            }
        }
    }
}
#endif
