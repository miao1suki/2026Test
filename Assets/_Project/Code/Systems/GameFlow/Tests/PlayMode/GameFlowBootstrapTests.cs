using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Project.GameFlow.Tests
{
    public sealed class GameFlowBootstrapTests
    {
        private const string BootstrapPath =
            "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
        private const string MainMenuPath =
            "Assets/_Project/Scenes/Flow/MainMenu.unity";

        private static readonly string[] PersistentPaths =
        {
            "Assets/_Project/Scenes/Systems/Systems_Core.unity",
            "Assets/_Project/Scenes/Systems/Systems_Input.unity",
            "Assets/_Project/Scenes/Systems/Systems_Audio.unity",
            "Assets/_Project/Scenes/Systems/Systems_Camera.unity",
            "Assets/_Project/Scenes/Systems/Systems_UI.unity",
        };

        [UnityTest]
        public IEnumerator Bootstrap_LoadsSystemsAndMainMenuWithoutDuplicates()
        {
            GameFlowLaunchOverride.Set(GameFlowSceneId.MainMenu);
            yield return SceneManager.LoadSceneAsync(
                BootstrapPath,
                LoadSceneMode.Additive);

            const int maximumFrames = 300;
            int frame = 0;
            while (frame < maximumFrames &&
                   (GameFlowController.Instance == null ||
                    !GameFlowController.Instance.IsInitialized ||
                    GameFlowController.Instance.ActiveSceneId !=
                    GameFlowSceneId.MainMenu))
            {
                frame++;
                yield return null;
            }

            GameFlowController flow = GameFlowController.Instance;
            Assert.That(flow, Is.Not.Null, "Bootstrap 未创建 GameFlowController。");
            Assert.That(flow.IsInitialized, Is.True, "系统场景未完成初始化。");
            Assert.That(flow.ActiveSceneId, Is.EqualTo(GameFlowSceneId.MainMenu));
            Assert.That(SceneManager.GetSceneByPath(BootstrapPath).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByPath(MainMenuPath).isLoaded, Is.True);
            for (int index = 0; index < PersistentPaths.Length; index++)
            {
                Assert.That(
                    SceneManager.GetSceneByPath(PersistentPaths[index]).isLoaded,
                    Is.True,
                    $"系统场景未加载：{PersistentPaths[index]}");
            }

            Assert.That(
                Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(1),
                "运行时只能有一个 Camera。");
            Assert.That(
                Object.FindObjectsByType<AudioListener>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(1),
                "运行时只能有一个 AudioListener。");
            Assert.That(
                Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(1),
                "运行时只能有一个 EventSystem。");
        }
    }
}
