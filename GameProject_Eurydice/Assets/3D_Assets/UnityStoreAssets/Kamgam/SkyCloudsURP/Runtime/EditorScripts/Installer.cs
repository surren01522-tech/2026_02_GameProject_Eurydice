using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
#endif
namespace Kamgam.SkyClouds.URP
{
    public class Installer
#if UNITY_EDITOR
        : IActiveBuildTargetChanged
#endif
    {
        public const string AssetName = "Sky Clouds URP";
        public const string Version = "1.2.0";
        public const string Define = "KAMGAM_SKY_CLOUDS_URP";
        public const string ManualUrl = "https://kamgam.com/unity/SkyCloudsURPManual.pdf";
        public const string AssetLink = "https://assetstore.unity.com/packages/slug/288138";

        public static string _assetRootPathDefault = "Assets/Kamgam/SkyCloudsURP/";
        public static string AssetRootPath
        {
			get
			{
#if UNITY_EDITOR
                if (System.IO.File.Exists(_assetRootPathDefault))
                {
                    return _assetRootPathDefault;
                }

                // The the tool was moved then search for the installer script and derive the root
                // path from there. Used Assets/ as ultimate fallback.
                string finalPath = "Assets/";
                string assetRootPathRelative = _assetRootPathDefault.Replace("Assets/", "");
                var installerGUIDS = AssetDatabase.FindAssets("t:Script Installer");
                foreach (var guid in installerGUIDS)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains(assetRootPathRelative))
                    {
                        int index = path.IndexOf(assetRootPathRelative);
                        return path.Substring(0, index) + assetRootPathRelative;
                    }
                }

                return finalPath;
#else
                return _assetRootPathDefault;
#endif
			}
        }
        public static string ExamplePath = AssetRootPath + "Examples/SkyCloudsDemo.unity";

        public static Version GetVersion() => new Version(Version);

#if UNITY_EDITOR
        [UnityEditor.Callbacks.DidReloadScripts(998001)]
        public static void InstallIfNeeded()
        {
            bool versionChanged = VersionHelper.UpgradeVersion(GetVersion, out Version oldVersion, out Version newVersion);
            if (versionChanged)
            {
                if (versionChanged)
                {
                    Debug.Log(AssetName + " version changed from " + oldVersion + " to " + newVersion);

                    if (AddDefineSymbol())
                    {
                        CrossCompileCallbacks.RegisterCallback(showWelcomeMessage);
                    }
                    else
                    {
                        showWelcomeMessage();
                    }
                }
            }
        }

        public int callbackOrder => 0;

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            Logger.LogMessage($"Build target changed from {previousTarget} to {newTarget}. Refreshing define symbols.");
            AddDefineSymbol();
        }

        [MenuItem("Tools/" + AssetName + "/Debug/Add Defines", priority = 501)]
        private static void AddDefineSymbolMenu()
        {
            AddDefineSymbol();
        }

        private static bool AddDefineSymbol()
        {
            bool didChange = false;

            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (isObsolete(targetGroup))
                    continue;
				
				if (targetGroup == BuildTargetGroup.Unknown)
					continue;

#if UNITY_2023_1_OR_NEWER
				string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
#else
				string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

				if (currentDefineSymbols.Contains(Define))
					continue;

#if UNITY_2023_1_OR_NEWER
				PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), currentDefineSymbols + ";" + Define);
#else
				PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, currentDefineSymbols + ";" + Define);
#endif
				// Logger.LogMessage($"{Define} symbol has been added for {targetGroup}.");

				didChange = true;
			}

            return didChange;
        }

        [MenuItem("Tools/" + AssetName + "/Debug/Remove Defines", priority = 502)]
        private static void RemoveDefineSymbol()
        {
            foreach (BuildTargetGroup targetGroup in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (isObsolete(targetGroup))
                    continue;

#if UNITY_2023_1_OR_NEWER
				string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup));
#else
				string currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif

				if (currentDefineSymbols.Contains(Define))
				{
					currentDefineSymbols = currentDefineSymbols.Replace(";" + Define, "");
#if UNITY_2023_1_OR_NEWER
					PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(targetGroup), currentDefineSymbols);
#else
					PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, currentDefineSymbols);
#endif
					Logger.LogMessage($"{Define} symbol has been removed for {targetGroup}.");
				}

            }
        }
		
		private static bool isObsolete(Enum value)
		{
			var fi = value.GetType().GetField(value.ToString());
			var attributes = (ObsoleteAttribute[]) fi.GetCustomAttributes(typeof(ObsoleteAttribute), inherit: false);
			return (attributes != null && attributes.Length > 0);
		}

        static void showWelcomeMessage()
        {
            bool openExample = EditorUtility.DisplayDialog(
                    AssetName,
                    "Thank you for choosing " + AssetName + ".\n\n" +
                    "Please start by reading the manual.\n\n" +
                    "If you can find the time I would appreciate your feedback in the form of a review.\n\n" +
                    "I have prepared some examples for you.",
                    "Open Example", "Open manual (web)"
                    );

            if (openExample)
                OpenExample();
            else
                OpenManual();
        }


        [MenuItem("Tools/" + AssetName + "/Manual", priority = 101)]
        public static void OpenManual()
        {
            Application.OpenURL(ManualUrl);
        }

        [MenuItem("Tools/" + AssetName + "/Open Example Scene", priority = 103)]
        public static void OpenExample()
        {
            EditorApplication.delayCall += () => 
            {
                var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ExamplePath);
                EditorGUIUtility.PingObject(scene);
                EditorSceneManager.OpenScene(ExamplePath);
            };
        }

        [MenuItem("Tools/" + AssetName + "/Please leave a review :-)", priority = 510)]
        public static void LeaveReview()
        {
            Application.OpenURL(AssetLink);
        }

        [MenuItem("Tools/" + AssetName + "/More Assets by KAMGAM", priority = 511)]
        public static void MoreAssets()
        {
            Application.OpenURL("https://kamgam.com/unity?ref=asset");
        }

        [MenuItem("Tools/" + AssetName + "/Version " + Version, priority = 512)]
        public static void LogVersion()
        {
            Debug.Log(AssetName + " v" + Version);
        }
#endif
    }
}