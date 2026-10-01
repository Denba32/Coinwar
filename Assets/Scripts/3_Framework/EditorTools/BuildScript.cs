using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class BuildScript
{
    private const string SteamAppId = "480"; // 실제 프로젝트 App ID로 교체
    private const string DevBuildSymbol = "DEV_BUILD";
    public static void Build()
    {
        string[] args = Environment.GetCommandLineArgs();

        string platform = GetArgument(args, "-platform") ?? "windows";

        BuildTarget target = platform switch
        {
            "android" => BuildTarget.Android,
            "windows" => BuildTarget.StandaloneWindows64,
            "ios" => BuildTarget.iOS,
            _ => throw new Exception("Unknown platform")
        };

        string extension = platform switch
        {
            "android" => "apk",
            "windows" => "exe",
            _ => ""
        };

        BuildPlayerOptions options = new BuildPlayerOptions();

        options.target = target;

        options.scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        options.locationPathName = $"Builds/build.{extension}";

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {

        }
        else
        {
            throw new Exception("BUILD FAILED");
        }
    }
    private static void AddDefineSymbol(string symbol)
    {
        var namedTarget = NamedBuildTarget.Standalone;
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out string[] defines);

        var symbolSet = new HashSet<string>(defines);
        if (symbolSet.Add(symbol)) // 이미 있으면 아무것도 안 함
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbolSet.ToArray());
        }
    }
    private static void RemoveDefineSymbol(string symbol)
    {
        PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, out string[] defines);

        var symbolSet = new HashSet<string>(defines);
        if (symbolSet.Remove(symbol)) // 있었으면 제거
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbolSet.ToArray());
        }
    }
    static string GetArgument(string[] args, string name)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    [MenuItem("Tools/Build/Dev Build (Windows)")]
    public static void BuildDev()
    {
        AddDefineSymbol(DevBuildSymbol);
        string buildPath = "Builds/Dev/Windows/CoinWar.exe";

        var options = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            CreateSteamAppIdFile(Path.GetDirectoryName(buildPath));
        }
    }

    [MenuItem("Tools/Build/Steam Release Build (Windows)")]
    public static void BuildSteamRelease()
    {
        RemoveDefineSymbol(DevBuildSymbol);

        var options = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = "Builds/Steam/Windows/CoinWar.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        // Steam Release는 steam_appid.txt를 만들지 않음 (Steam 클라이언트 경유 실행 전제)
        BuildPipeline.BuildPlayer(options);
    }

    private static string[] GetScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
    }
    private static void CreateSteamAppIdFile(string buildFolder)
    {
        string filePath = Path.Combine(buildFolder, "steam_appid.txt");
        File.WriteAllText(filePath, SteamAppId);
        UnityEngine.Debug.Log($"steam_appid.txt 생성 완료: {filePath}");
    }
}