using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PWRISimulator.Editor
{
    /// <summary>
    /// Batch build entry point for the GitHub Actions Windows simulator build.
    /// Invoked via -executeMethod PWRISimulator.Editor.HackathonBuild.BuildWindows.
    /// Validates assumptions, temporarily applies distribution ROS settings
    /// (ConnectOnStart=false, ShowHUD=true) to ROSConnectionPrefab, builds
    /// GameScene for StandaloneWindows64/Mono, and restores the prefab bytes
    /// in finally so no persistent project change remains.
    /// </summary>
    public static class HackathonBuild
    {
        private const string ExpectedUnityVersion = "2022.3.62f3";
        private const string ScenePath = "Assets/Scenes/GameScene.unity";
        private const string PrefabAssetPath = "Assets/Resources/ROSConnectionPrefab.prefab";
        private const string PlayerDirectoryEnv = "OPERA_PLAYER_DIRECTORY";
        private const string PlayerExeName = "OperaSim.exe";

        public static void BuildWindows()
        {
            var exitCode = 0;
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var prefabFullPath = Path.GetFullPath(PrefabAssetPath, projectRoot);
            byte[] prefabBackup = null;
            try
            {
                ValidateEnvironment();
                prefabBackup = File.ReadAllBytes(prefabFullPath);

                ApplyDistributionPrefabSettings();
                AssetDatabase.ImportAsset(PrefabAssetPath, ImportAssetOptions.ForceUpdate);

                var report = BuildPlayer();
                if (report.summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"HackathonBuild: build finished with {report.summary.result} " +
                                   $"({report.summary.totalErrors} error(s)).");
                    exitCode = 1;
                }
                else
                {
                    Debug.Log($"HackathonBuild: build succeeded -> {report.summary.outputPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                exitCode = 1;
            }
            finally
            {
                if (prefabBackup != null)
                {
                    try
                    {
                        RestorePrefab(prefabFullPath, prefabBackup);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        exitCode = 1;
                    }
                }
            }

            if (exitCode != 0)
            {
                if (Application.isBatchMode)
                {
                    // Restoration already ran; report failure to the batch Editor.
                    EditorApplication.Exit(exitCode);
                }
                else
                {
                    // Interactive callers must see the failure; never close
                    // their Editor.
                    throw new InvalidOperationException(
                        "HackathonBuild.BuildWindows failed; see the log for details.");
                }
            }
        }

        private static void ValidateEnvironment()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                throw new InvalidOperationException(
                    $"HackathonBuild requires the Windows Editor, got {Application.platform}.");
            }
            if (Application.unityVersion != ExpectedUnityVersion)
            {
                throw new InvalidOperationException(
                    $"HackathonBuild requires Unity {ExpectedUnityVersion}, got {Application.unityVersion}.");
            }
            if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Standalone) != ScriptingImplementation.Mono2x)
            {
                throw new InvalidOperationException(
                    "HackathonBuild requires the Standalone Mono scripting backend.");
            }
            var standaloneTarget = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
            var defines = PlayerSettings.GetScriptingDefineSymbols(standaloneTarget);
            if (defines == null || !defines.Split(';').Contains("ROS2"))
            {
                throw new InvalidOperationException(
                    "HackathonBuild requires the ROS2 scripting define for Standalone.");
            }
            if (!AGXUnityEditor.EditorSettings.Instance.BuildPlayer_CopyBinaries)
            {
                throw new InvalidOperationException(
                    "AGXUnity EditorSettings.BuildPlayer_CopyBinaries must be enabled.");
            }
        }

        private static void ApplyDistributionPrefabSettings()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabAssetPath);
            var found = false;
            try
            {
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    // Missing-script placeholders compare equal to null in
                    // Unity; skip them instead of building a SerializedObject.
                    if (behaviour == null)
                    {
                        continue;
                    }
                    var so = new SerializedObject(behaviour);
                    var connectOnStart = so.FindProperty("m_ConnectOnStart");
                    var showHud = so.FindProperty("m_ShowHUD");
                    if (connectOnStart == null || showHud == null)
                    {
                        continue;
                    }
                    connectOnStart.boolValue = false;
                    showHud.boolValue = true;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    found = true;
                }
                if (!found)
                {
                    throw new InvalidOperationException(
                        "ROS connection settings (m_ConnectOnStart/m_ShowHUD) not found in " + PrefabAssetPath);
                }
                PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RestorePrefab(string prefabFullPath, byte[] originalBytes)
        {
            File.WriteAllBytes(prefabFullPath, originalBytes);
            AssetDatabase.ImportAsset(PrefabAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static BuildReport BuildPlayer()
        {
            var playerDirectory = Environment.GetEnvironmentVariable(PlayerDirectoryEnv);
            if (string.IsNullOrWhiteSpace(playerDirectory))
            {
                throw new InvalidOperationException(
                    $"Environment variable {PlayerDirectoryEnv} is not set.");
            }
            Directory.CreateDirectory(playerDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(playerDirectory, PlayerExeName),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };
            return BuildPipeline.BuildPlayer(options);
        }
    }
}
