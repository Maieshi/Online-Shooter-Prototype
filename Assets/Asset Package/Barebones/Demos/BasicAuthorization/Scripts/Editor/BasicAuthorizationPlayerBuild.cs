using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Barebones.MasterServer.Examples.BasicAuthorization
{
    public class BasicAuthorizationPlayerBuild
    {
        [MenuItem("Tools/MSF/Build/Demos/Basic Authorization/All")]
        private static void BuildBoth()
        {
            BuildMaster();
            BuildClient();
        }

        [MenuItem("Tools/MSF/Build/Demos/Basic Authorization/Master Server")]
        private static void BuildMaster()
        {
            string buildFolder = Path.Combine("Builds", "BasicAuthorization", "MasterServer");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Barebones/Demos/BasicAuthorization/Scenes/MasterServer/MasterServer.unity" },
                locationPathName = Path.Combine(buildFolder, "MasterServer.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.EnableHeadlessMode | BuildOptions.ShowBuiltPlayer | BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                File.WriteAllText(Path.Combine(buildFolder, "Start Server.bat"), "start MasterServer.exe -msfStartMaster");

                Debug.Log("Server build succeeded: " + (summary.totalSize / 1024) + " kb");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Server build failed");
            }
        }

        [MenuItem("Tools/MSF/Build/Demos/Basic Authorization/Client")]
        private static void BuildClient()
        {
            string buildFolder = Path.Combine("Builds", "BasicAuthorization", "Client");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Barebones/Demos/BasicAuthorization/Scenes/Client/Client.unity" },
                locationPathName = Path.Combine(buildFolder, "Client.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.ShowBuiltPlayer | BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Client build succeeded: " + (summary.totalSize / 1024) + " kb");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.Log("Client build failed");
            }
        }
    }
}