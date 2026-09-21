using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TimelineCamRig))]
public class TimelineCamRigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TimelineHelp.DrawBoxFor(target);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("Project 相机", EditorStyles.boldLabel);
        SerializedProperty managerProperty =
            serializedObject.FindProperty("cameraManager");
        EditorGUILayout.PropertyField(
            managerProperty,
            new GUIContent("相机管理器", "Project CameraControlManager，是唯一允许写入 Camera 的组件"));
        if (managerProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(
                "请把本组件挂到 CameraControlManager 所在相机上，或手动指定管理器。",
                MessageType.Warning);
        }
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("controlPriority"),
            new GUIContent("控制优先级", "默认使用 Project 的 Cutscene 优先级"));
        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("returnTransitionDuration"),
            new GUIContent("交还过渡时间", "轨道结束后平滑回到 Project 玩法相机的时间"));

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("跟随目标", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("target"), new GUIContent("跟随目标"));
        if (serializedObject.FindProperty("target").objectReferenceValue == null)
        {
            EditorGUILayout.LabelField("未指定目标：使用 Project 当前焦点作为运镜锚点", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("环绕机位", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotX"), new GUIContent("俯仰角"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotY"), new GUIContent("环绕角"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("distance"), new GUIContent("相机距离"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("相机高度"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lookHeight"), new GUIContent("看向抬高"));

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("状态", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isPlayingAnim"), new GUIContent("运镜接管中"));

        serializedObject.ApplyModifiedProperties();
    }
}
