using System.Collections;
using NUnit.Framework;
using Project.CameraModes;
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

            GameUiRouter router = Object.FindFirstObjectByType<GameUiRouter>();
            Assert.That(router, Is.Not.Null);
            router.StartGame();
            yield return WaitForScene(flow, GameFlowSceneId.Level01);
            AssertPlayerAndVisibleGameplayUi(GameFlowSceneId.Level01);

            router.SetPaused(true, false);
            Assert.That(router.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(router.PauseScreen.activeSelf, Is.True);
            router.SetPaused(false, false);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            yield return CompleteCurrentLevel(flow, GameFlowSceneId.Level02);
            AssertPlayerAndVisibleGameplayUi(GameFlowSceneId.Level02);
            yield return CompleteCurrentLevel(flow, GameFlowSceneId.Level03);
            AssertPlayerAndVisibleGameplayUi(GameFlowSceneId.Level03);
            yield return CompleteCurrentLevel(flow, GameFlowSceneId.Ending);
            Assert.That(
                Object.FindObjectsByType<LevelGoal>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(0));
        }

        private static IEnumerator CompleteCurrentLevel(
            GameFlowController flow,
            GameFlowSceneId expectedTarget)
        {
            LevelGoal goal = Object.FindFirstObjectByType<LevelGoal>();
            Assert.That(goal, Is.Not.Null, "当前关卡缺少 LevelGoal。");
            goal.Configure(
                goal.NextScene,
                goal.EndsGame,
                0f,
                goal.transform);
            Assert.That(goal.BeginCompletion(), Is.True);
            yield return WaitForScene(flow, expectedTarget);
        }

        private static IEnumerator WaitForScene(
            GameFlowController flow,
            GameFlowSceneId expected)
        {
            const int maximumFrames = 600;
            int frame = 0;
            while (frame < maximumFrames &&
                   (flow.IsTransitioning || flow.ActiveSceneId != expected))
            {
                frame++;
                yield return null;
            }
            Assert.That(flow.ActiveSceneId, Is.EqualTo(expected));
            Assert.That(flow.IsTransitioning, Is.False);
        }

        private static void AssertPlayerAndVisibleGameplayUi(GameFlowSceneId id)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            Assert.That(player, Is.Not.Null, $"{id} 没有生成玩家。");
            Assert.That(
                player.scene,
                Is.EqualTo(SceneManager.GetActiveScene()),
                "玩家必须属于当前关卡场景。");
            CameraFollowController follow =
                Object.FindFirstObjectByType<CameraFollowController>();
            Assert.That(follow, Is.Not.Null);
            Assert.That(
                follow.Target,
                Is.EqualTo(player.transform),
                "全局相机必须跟随当前关卡生成的玩家。");

            GameUiRouter router = Object.FindFirstObjectByType<GameUiRouter>();
            Assert.That(router, Is.Not.Null);
            Transform gameplay = router.transform.Find("GameplayHud");
            Transform loading = router.transform.Find("LoadingScreen");
            Assert.That(gameplay, Is.Not.Null);
            Assert.That(gameplay.gameObject.activeSelf, Is.True);
            Assert.That(loading, Is.Not.Null);
            Assert.That(
                loading.gameObject.activeSelf,
                Is.False,
                "进入关卡后加载提示必须关闭，不能遮挡场景。");
        }
    }
}
