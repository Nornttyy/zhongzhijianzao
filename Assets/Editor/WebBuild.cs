using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DoNotOpen.EditorTools
{
    public static class WebBuild
    {
        private const string ScenePath = "Assets/Scenes/Prototype.unity";
        private const string DefaultOutputPath = "Builds/WebGL";

        [MenuItem("Do Not Open/Build Web Version")]
        public static void BuildWebVersion()
        {
            string outputPath = ReadArgument("-webBuildPath") ?? DefaultOutputPath;
            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(outputPath);

            PlayerSettings.productName = "Do Not Open";
            PlayerSettings.companyName = "Nornttyy";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;

            // GitHub Pages doesn't allow custom Content-Encoding headers. The fallback
            // keeps the download compressed while letting Unity decompress in-browser.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:DoNotOpen";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Web build failed: {summary.result}, {summary.totalErrors} error(s).");
            }

            Debug.Log($"Web build completed: {outputPath} ({summary.totalSize} bytes)");
        }

        private static string ReadArgument(string name)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] == name)
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }
    }
}
