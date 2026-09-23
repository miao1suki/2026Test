using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Project.LevelAuthoring.Editor
{
    internal sealed class LevelReleaseBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (!scenes[index].enabled)
                {
                    continue;
                }

                bool developmentScene =
                    scenes[index].path.Contains("/_Project/Development/");
                bool legacyLevelScene =
                    scenes[index].path.Contains("/_Project/Scenes/Levels/");
                if (developmentScene || legacyLevelScene)
                {
                    throw new BuildFailedException(
                        "Build Settings 中包含开发或旧版关卡场景：" +
                        scenes[index].path +
                        "。正式包只能使用 Assets/_Project/Release 下的关卡场景。");
                }
            }
        }
    }
}
