
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class DemoBuildTools
{
    private const string BuildIdFolder = "Assets/StreamingAssets";
    private const string BuildIdFile = "Assets/StreamingAssets/build_id.txt";

    [MenuItem("Tools/Windows Demo/Build Windows Demo")]
    public static void BuildWindowsDemo()
    {
        // 获取 Build Settings 中启用的场景
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        string[] scenePaths = Array.ConvertAll(Array.FindAll(scenes, scene => scene.enabled),scene => scene.path);

        if (scenePaths.Length == 0)
        {
            Debug.LogError("构建失败：Build Settings 中没有启用的场景！");
            return;
        }

        // 确保 StreamingAssets 文件夹存在
        if (!Directory.Exists(BuildIdFolder))
        {
            Directory.CreateDirectory(BuildIdFolder);
        }

        // 每次构建生成一个新的 ID
        string buildId = Guid.NewGuid().ToString("N");
        File.WriteAllText(BuildIdFile, buildId);

        // 选择生成的 exe 路径
        string outputPath = EditorUtility.SaveFilePanel("选择 Windows Demo 保存位置","","MyGameDemo.exe","exe");

        if (string.IsNullOrEmpty(outputPath))
        {
            Debug.LogWarning("已取消构建。下次构建时会重新生成 Build ID。");
            return;
        }

        AssetDatabase.Refresh();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Windows Demo 构建成功！\n路径：{outputPath}\nBuild ID：{buildId}");
            EditorUtility.RevealInFinder(outputPath);
        }
        else
        {
            Debug.LogError($"Windows Demo 构建失败：{report.summary.result}");
        }
    }
}
