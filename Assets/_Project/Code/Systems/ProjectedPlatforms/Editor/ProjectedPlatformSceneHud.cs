using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.ProjectedPlatforms.Editor
{
    [InitializeOnLoad]
    internal static class ProjectedPlatformSceneHud
    {
        private const string RootName = "projected-platform-scene-hud";
        private const string VisiblePreference =
            "2026Test.ProjectedPlatforms.Visible";

        static ProjectedPlatformSceneHud()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= RemoveAll;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveAll;
        }

        private static bool IsVisible
        {
            get => EditorPrefs.GetBool(VisiblePreference, false);
            set => EditorPrefs.SetBool(VisiblePreference, value);
        }

        [MenuItem("Tools/2026Test/正交单向平台/显示 Scene 工具")]
        private static void ShowTool()
        {
            IsVisible = true;
            SceneView.lastActiveSceneView?.Focus();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/2026Test/正交单向平台/让选中物体成为单向平台")]
        private static void EnableSelected()
        {
            ProjectedPlatformAuthoringService.Enable(Selection.activeGameObject);
            ShowTool();
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                sceneView.rootVisualElement.Q<VisualElement>(RootName)
                    ?.RemoveFromHierarchy();
                return;
            }

            VisualElement root = sceneView.rootVisualElement.Q<VisualElement>(RootName);
            if (root == null || !(root.userData is HudElements))
            {
                root?.RemoveFromHierarchy();
                root = CreateRoot();
                sceneView.rootVisualElement.Add(root);
            }

            root.style.display = IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (IsVisible)
            {
                ((HudElements)root.userData).Refresh();
            }
        }

        private static VisualElement CreateRoot()
        {
            VisualElement root = new VisualElement { name = RootName };
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.right = 12f;
            root.style.top = 380f;
            root.userData = new HudElements(root);
            return root;
        }

        private static void RemoveAll()
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                sceneView.rootVisualElement.Q<VisualElement>(RootName)
                    ?.RemoveFromHierarchy();
            }
        }

        private sealed class HudElements
        {
            private readonly VisualElement panel;
            private readonly VisualElement body;
            private readonly Label status;
            private readonly Button enable;
            private readonly VisualElement settings;
            private readonly EnumFlagsField directions;
            private readonly FloatField tolerance;
            private readonly FloatField projectionDepth;
            private bool refreshing;

            internal HudElements(VisualElement root)
            {
                panel = new VisualElement { pickingMode = PickingMode.Position };
                panel.style.width = 310f;
                panel.style.paddingLeft = 10f;
                panel.style.paddingRight = 10f;
                panel.style.paddingTop = 8f;
                panel.style.paddingBottom = 8f;
                panel.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.09f, 0.075f, 0.12f, 0.97f)
                    : new Color(0.97f, 0.95f, 0.99f, 0.98f);
                Round(panel, 8f);
                root.Add(panel);

                VisualElement header = Row();
                header.style.alignItems = Align.Center;
                VisualElement titles = new VisualElement();
                titles.style.flexGrow = 1f;
                Label title = new Label("正交单向平台");
                title.style.fontSize = 14f;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                titles.Add(title);
                Label subtitle = new Label("独立碰撞功能 · 不依赖方块贴画");
                subtitle.style.fontSize = 9f;
                subtitle.style.opacity = 0.65f;
                titles.Add(subtitle);
                header.Add(titles);

                body = new VisualElement();
                Button collapse = SmallButton("▾", null);
                collapse.clicked += () =>
                {
                    bool show = body.style.display.value == DisplayStyle.None;
                    body.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                    collapse.text = show ? "▾" : "▸";
                };
                header.Add(collapse);
                header.Add(SmallButton("×", () =>
                {
                    IsVisible = false;
                    SceneView.RepaintAll();
                }));
                panel.Add(header);
                panel.Add(body);
                MakeDraggable(panel, header);

                status = Badge("请选择需要配置的平台物体");
                body.Add(status);
                enable = BigButton("让选中物体成为单向平台", () =>
                {
                    ProjectedPlatformAuthoringService.Enable(
                        Selection.activeGameObject);
                    SceneView.RepaintAll();
                });
                body.Add(enable);

                settings = new VisualElement();
                directions = new EnumFlagsField(
                    "生效的2D视角",
                    ProjectedPlatformDirections.All);
                directions.RegisterValueChangedCallback(evt =>
                {
                    ProjectedOneWayPlatform platform = SelectedPlatform();
                    if (refreshing || platform == null)
                    {
                        return;
                    }

                    Undo.RecordObject(platform, "修改单向平台方向");
                    platform.Directions = (ProjectedPlatformDirections)evt.newValue;
                    Dirty(platform);
                });
                settings.Add(directions);

                tolerance = new FloatField("落脚容差") { isDelayed = true };
                tolerance.RegisterValueChangedCallback(evt =>
                {
                    ProjectedOneWayPlatform platform = SelectedPlatform();
                    if (refreshing || platform == null)
                    {
                        return;
                    }

                    Undo.RecordObject(platform, "修改单向平台容差");
                    platform.LandingTolerance = evt.newValue;
                    Dirty(platform);
                });
                settings.Add(tolerance);
                projectionDepth = new FloatField("投影纵深") { isDelayed = true };
                projectionDepth.tooltip =
                    "2D 模式临时碰撞沿视角深度延伸的世界长度。关卡更大时再调高。";
                projectionDepth.RegisterValueChangedCallback(evt =>
                {
                    ProjectedOneWayPlatform platform = SelectedPlatform();
                    if (refreshing || platform == null)
                    {
                        return;
                    }

                    Undo.RecordObject(platform, "修改平台投影纵深");
                    platform.ProjectionDepth = evt.newValue;
                    Dirty(platform);
                });
                settings.Add(projectionDepth);
                settings.Add(Button("移除单向平台功能", () =>
                {
                    ProjectedPlatformAuthoringService.Disable(SelectedPlatform());
                    SceneView.RepaintAll();
                }));
                Label help = new Label(
                    "匹配视角下会沿视角纵深建立临时承载面，可从下方穿过并从上方落脚；3D 和未勾选视角保持普通实体碰撞。移动平台也使用同一规则。仅需要 BoxCollider，不需要 SurfaceTileBlock。");
                help.style.whiteSpace = WhiteSpace.Normal;
                help.style.fontSize = 9f;
                help.style.opacity = 0.72f;
                help.style.marginTop = 5f;
                settings.Add(help);
                body.Add(settings);
            }

            internal void Refresh()
            {
                GameObject selected = Selection.activeGameObject;
                ProjectedOneWayPlatform platform = SelectedPlatform();
                refreshing = true;
                status.text = selected == null
                    ? "未选中场景物体"
                    : platform == null
                        ? $"{selected.name} · 普通物体"
                        : $"{platform.name} · 已启用单向平台";
                enable.style.display = platform == null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                settings.style.display = platform != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                if (platform != null)
                {
                    directions.SetValueWithoutNotify(platform.Directions);
                    tolerance.SetValueWithoutNotify(platform.LandingTolerance);
                    projectionDepth.SetValueWithoutNotify(platform.ProjectionDepth);
                }

                refreshing = false;
            }

            private static ProjectedOneWayPlatform SelectedPlatform()
            {
                return Selection.activeGameObject != null
                    ? Selection.activeGameObject
                        .GetComponentInParent<ProjectedOneWayPlatform>()
                    : null;
            }

            private static void Dirty(ProjectedOneWayPlatform platform)
            {
                EditorUtility.SetDirty(platform);
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
                SceneView.RepaintAll();
            }

            private static VisualElement Row()
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                return row;
            }

            private static Label Badge(string text)
            {
                Label label = new Label(text);
                label.style.paddingLeft = 7f;
                label.style.paddingRight = 7f;
                label.style.paddingTop = 4f;
                label.style.paddingBottom = 4f;
                label.style.marginTop = 5f;
                label.style.marginBottom = 4f;
                label.style.backgroundColor = new Color(0.22f, 0.15f, 0.3f, 0.86f);
                Round(label, 4f);
                return label;
            }

            private static Button BigButton(string text, Action action)
            {
                Button button = Button(text, action);
                button.style.height = 36f;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
                button.style.backgroundColor = new Color(0.48f, 0.28f, 0.78f, 1f);
                button.style.color = Color.white;
                return button;
            }

            private static Button Button(string text, Action action)
            {
                Button button = new Button(action) { text = text };
                button.style.height = 26f;
                button.style.marginTop = 2f;
                button.style.marginBottom = 2f;
                return button;
            }

            private static Button SmallButton(string text, Action action)
            {
                Button button = action == null ? new Button() : new Button(action);
                button.text = text;
                button.style.width = 25f;
                button.style.height = 22f;
                button.style.marginLeft = 3f;
                return button;
            }

            private static void Round(VisualElement element, float radius)
            {
                element.style.borderTopLeftRadius = radius;
                element.style.borderTopRightRadius = radius;
                element.style.borderBottomLeftRadius = radius;
                element.style.borderBottomRightRadius = radius;
            }

            private static void MakeDraggable(
                VisualElement target,
                VisualElement handle)
            {
                bool dragging = false;
                int pointerId = -1;
                Vector2 last = Vector2.zero;
                handle.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0 || evt.target is Button)
                    {
                        return;
                    }

                    Rect layout = target.layout;
                    target.style.left = layout.x;
                    target.style.top = layout.y;
                    target.style.right = StyleKeyword.Auto;
                    dragging = true;
                    pointerId = evt.pointerId;
                    last = evt.position;
                    handle.CapturePointer(pointerId);
                    evt.StopPropagation();
                });
                handle.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!dragging || evt.pointerId != pointerId)
                    {
                        return;
                    }

                    Vector2 current = evt.position;
                    Vector2 delta = current - last;
                    last = current;
                    target.style.left = target.layout.x + delta.x;
                    target.style.top = target.layout.y + delta.y;
                    evt.StopPropagation();
                });
                handle.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!dragging || evt.pointerId != pointerId)
                    {
                        return;
                    }

                    dragging = false;
                    handle.ReleasePointer(pointerId);
                    pointerId = -1;
                    evt.StopPropagation();
                });
                handle.RegisterCallback<PointerCaptureOutEvent>(_ =>
                {
                    dragging = false;
                    pointerId = -1;
                });
            }
        }
    }
}
