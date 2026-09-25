using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace Project.Player.Editor
{
    [CustomEditor(typeof(PlayerActionRunner))]
    public sealed class PlayerActionRunnerEditor : UnityEditor.Editor
    {
        private SerializedProperty script;
        private SerializedProperty director;

        private void OnEnable()
        {
            script = serializedObject.FindProperty("m_Script");
            director = serializedObject.FindProperty("director");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(
                    script,
                    new GUIContent("脚本"));
            }

            EditorGUILayout.PropertyField(
                director,
                new GUIContent("播放导演", "留空时自动查找同物体上的 PlayableDirector"));

            PlayerActionRunner runner =
                (PlayerActionRunner)target;
            PlayableDirector playableDirector =
                runner.GetComponent<PlayableDirector>();
            if (director.objectReferenceValue == null &&
                playableDirector != null)
            {
                director.objectReferenceValue = playableDirector;
            }

            if (playableDirector == null)
            {
                EditorGUILayout.HelpBox(
                    "缺少 PlayableDirector，无法播放动作 Timeline。",
                    MessageType.Error);
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(
                "运行观察",
                EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField(
                    "当前动作",
                    runner.CurrentAction,
                    typeof(ActSO),
                    false);
                EditorGUILayout.Toggle(
                    "正在播放",
                    runner.IsPlaying);
                if (playableDirector != null)
                {
                    EditorGUILayout.ObjectField(
                        "播放资源",
                        playableDirector.playableAsset,
                        typeof(UnityEngine.Object),
                        false);
                    EditorGUILayout.EnumPopup(
                        "导演状态",
                        playableDirector.state);
                    EditorGUILayout.FloatField(
                        "当前时间",
                        (float)playableDirector.time);
                    EditorGUILayout.FloatField(
                        "总时长",
                        (float)playableDirector.duration);
                }
            }
        }
    }
}
