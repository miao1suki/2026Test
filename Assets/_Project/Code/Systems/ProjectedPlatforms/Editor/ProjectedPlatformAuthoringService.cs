using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.ProjectedPlatforms.Editor
{
    internal static class ProjectedPlatformAuthoringService
    {
        internal static ProjectedOneWayPlatform Enable(GameObject target)
        {
            if (target == null)
            {
                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = "ProjectedOneWayPlatform";
                target.transform.position = SceneView.lastActiveSceneView != null
                    ? SceneView.lastActiveSceneView.pivot
                    : Vector3.zero;
                Undo.RegisterCreatedObjectUndo(target, "创建正交单向平台");
            }

            if (target.GetComponent<BoxCollider>() == null)
            {
                Undo.AddComponent<BoxCollider>(target);
            }

            ProjectedOneWayPlatform platform =
                target.GetComponent<ProjectedOneWayPlatform>();
            if (platform == null)
            {
                platform = Undo.AddComponent<ProjectedOneWayPlatform>(target);
            }

            Transform existingSensor =
                target.transform.Find("__ProjectedPlatformSensor");
            Transform existingProxy =
                target.transform.Find("__ProjectedCollisionProxy");
            Undo.RegisterFullObjectHierarchyUndo(target, "配置正交单向平台");
            platform.EnsureSetup();
            Transform sensor = target.transform.Find("__ProjectedPlatformSensor");
            if (existingSensor == null && sensor != null)
            {
                Undo.RegisterCreatedObjectUndo(sensor.gameObject, "创建平台检测范围");
            }

            Transform proxy = target.transform.Find("__ProjectedCollisionProxy");
            if (existingProxy == null && proxy != null)
            {
                Undo.RegisterCreatedObjectUndo(proxy.gameObject, "创建正交投影碰撞");
            }

            EditorUtility.SetDirty(platform);
            EditorSceneManager.MarkSceneDirty(target.scene);
            Selection.activeGameObject = target;
            return platform;
        }

        internal static void Disable(ProjectedOneWayPlatform platform)
        {
            if (platform == null)
            {
                return;
            }

            GameObject target = platform.gameObject;
            Transform sensor = target.transform.Find("__ProjectedPlatformSensor");
            if (sensor != null)
            {
                Undo.DestroyObjectImmediate(sensor.gameObject);
            }

            Transform proxy = target.transform.Find("__ProjectedCollisionProxy");
            if (proxy != null)
            {
                Undo.DestroyObjectImmediate(proxy.gameObject);
            }

            Undo.DestroyObjectImmediate(platform);
            EditorSceneManager.MarkSceneDirty(target.scene);
        }
    }
}
