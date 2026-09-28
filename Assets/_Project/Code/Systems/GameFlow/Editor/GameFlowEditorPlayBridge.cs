using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.GameFlow.Editor
{
    [InitializeOnLoad]
    public static class GameFlowEditorPlayBridge
    {
        private const string MenuPath =
            "Tools/2026Test/游戏流程/Play时自动从Bootstrap启动";
        private const string PreferenceKey =
            "2026Test.GameFlow.AutoBootstrapOnPlay";
        private const string RequestedSceneKey =
            "2026Test.GameFlow.RequestedScene";

        static GameFlowEditorPlayBridge()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += ApplyStartScenePreference;
        }

        [MenuItem(MenuPath, priority = 30)]
        private static void ToggleAutoBootstrap()
        {
            bool next = !IsEnabled;
            EditorPrefs.SetBool(PreferenceKey, next);
            ApplyStartScenePreference();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggleAutoBootstrap()
        {
            Menu.SetChecked(MenuPath, IsEnabled);
            return true;
        }

        public static void ApplyStartScenePreference()
        {
            if (Application.isBatchMode)
            {
                EditorSceneManager.playModeStartScene = null;
                return;
            }

            SceneAsset bootstrap = IsEnabled
                ? AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    GameFlowSceneScaffolder.BootstrapPath)
                : null;
            EditorSceneManager.playModeStartScene = bootstrap;
            Menu.SetChecked(MenuPath, IsEnabled);
        }

        private static bool IsEnabled => EditorPrefs.GetBool(PreferenceKey, true);

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                CaptureRequestedFlowScene();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                GameFlowSceneId requested = (GameFlowSceneId)SessionState.GetInt(
                    RequestedSceneKey,
                    (int)GameFlowSceneId.MainMenu);
                GameFlowLaunchOverride.Set(requested);
            }
        }

        private static void CaptureRequestedFlowScene()
        {
            GameFlowSceneId requested = GameFlowSceneId.MainMenu;
            GameSceneCatalog catalog =
                AssetDatabase.LoadAssetAtPath<GameSceneCatalog>(
                    GameFlowSceneScaffolder.CatalogPath);
            string activePath = EditorSceneManager.GetActiveScene().path;
            if (catalog != null &&
                catalog.TryGetIdForPath(activePath, out GameFlowSceneId sceneId))
            {
                requested = sceneId;
            }

            SessionState.SetInt(RequestedSceneKey, (int)requested);
        }
    }
}
