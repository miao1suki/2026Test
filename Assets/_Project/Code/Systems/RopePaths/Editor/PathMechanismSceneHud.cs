using System;
using Project.LadderPaths;
using Project.PlatformPaths;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.RopePaths.Editor
{
    [InitializeOnLoad]
    internal static class PathMechanismSceneHud
    {
        private const string RootName = "path-mechanism-scene-hud";
        private const string VisiblePreference = "2026Test.PathMechanisms.Visible";

        static PathMechanismSceneHud()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= RemoveAll;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveAll;
        }

        private static bool IsVisible
        {
            get => EditorPrefs.GetBool(VisiblePreference, true);
            set => EditorPrefs.SetBool(VisiblePreference, value);
        }

        [MenuItem("Tools/2026Test/路径机关/显示傻瓜式 Scene 工具")]
        private static void ShowTool()
        {
            IsVisible = true;
            SceneView.lastActiveSceneView?.Focus();
            SceneView.RepaintAll();
        }

        [MenuItem("Tools/2026Test/路径机关/新建绳子")]
        private static void CreateRopeFromMenu() =>
            PathMechanismAuthoringService.CreateRope();

        [MenuItem("Tools/2026Test/路径机关/新建梯子")]
        private static void CreateLadderFromMenu() =>
            PathMechanismAuthoringService.CreateLadder();

        [MenuItem("Tools/2026Test/路径机关/新建并绑定平台")]
        private static void CreatePlatformFromMenu() =>
            PathMechanismAuthoringService.CreateBoundPlatform();

        [MenuItem("Tools/2026Test/路径机关/检查并修复当前场景")]
        private static void RepairFromMenu()
        {
            ShowRepairResult(PathMechanismAuthoringService.RepairActiveScene());
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
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
            root.style.top = 46f;
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

        private static void Run(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                action();
                SceneView.RepaintAll();
            };
        }

        private static void ShowRepairResult(
            PathMechanismAuthoringService.RepairReport report)
        {
            string warning = report.UnboundPlatforms > 0
                ? $"\n仍有 {report.UnboundPlatforms} 个平台未绑定，请选中后点击‘绑定到最近绳端’。"
                : "\n所有平台均已绑定。";
            EditorUtility.DisplayDialog(
                "路径机关检查完成",
                $"绳子 {report.Ropes} · 梯子 {report.Ladders} · 平台 {report.Platforms}\n" +
                $"自动修复 {report.Repaired} 项。{warning}",
                "确定");
        }

        private sealed class HudElements
        {
            private readonly VisualElement panel;
            private readonly VisualElement body;
            private readonly Label status;
            private readonly Label selectionHint;
            private readonly VisualElement ropeActions;
            private readonly VisualElement ladderActions;
            private readonly VisualElement platformActions;
            private readonly EnumField direction;
            private readonly FloatField ropeTolerance;
            private readonly FloatField ladderTolerance;

            internal HudElements(VisualElement root)
            {
                panel = new VisualElement();
                panel.pickingMode = PickingMode.Position;
                panel.style.width = 324f;
                panel.style.paddingLeft = 10f;
                panel.style.paddingRight = 10f;
                panel.style.paddingTop = 8f;
                panel.style.paddingBottom = 8f;
                panel.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.075f, 0.09f, 0.12f, 0.97f)
                    : new Color(0.95f, 0.96f, 0.98f, 0.98f);
                panel.style.borderTopLeftRadius = 8f;
                panel.style.borderTopRightRadius = 8f;
                panel.style.borderBottomLeftRadius = 8f;
                panel.style.borderBottomRightRadius = 8f;
                root.Add(panel);

                VisualElement header = Row();
                header.style.alignItems = Align.Center;
                VisualElement titleGroup = new VisualElement();
                titleGroup.style.flexGrow = 1f;
                Label title = new Label("路径机关工具");
                title.style.fontSize = 14f;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleGroup.Add(title);
                Label subtitle = new Label("绳子 · 梯子 · 移动平台，一键生成");
                subtitle.style.fontSize = 9f;
                subtitle.style.opacity = 0.65f;
                titleGroup.Add(subtitle);
                header.Add(titleGroup);

                body = new VisualElement();
                Button collapse = SmallButton("▾", null);
                collapse.clicked += () =>
                {
                    bool collapsed = body.style.display.value == DisplayStyle.None;
                    body.style.display = collapsed ? DisplayStyle.Flex : DisplayStyle.None;
                    collapse.text = collapsed ? "▾" : "▸";
                };
                Button close = SmallButton("×", () =>
                {
                    IsVisible = false;
                    SceneView.RepaintAll();
                });
                header.Add(collapse);
                header.Add(close);
                panel.Add(header);
                panel.Add(body);
                MakeDraggable(panel, header);

                status = Badge("当前场景还没有路径机关");
                body.Add(status);

                Label step = new Label("1. 直接创建（网络会自动建立）");
                step.style.unityFontStyleAndWeight = FontStyle.Bold;
                step.style.marginTop = 7f;
                body.Add(step);
                VisualElement createRow = Row();
                createRow.Add(BigButton("＋ 绳子", () => Run(() =>
                    PathMechanismAuthoringService.CreateRope())));
                createRow.Add(BigButton("＋ 梯子", () => Run(() =>
                    PathMechanismAuthoringService.CreateLadder())));
                createRow.Add(BigButton("＋ 平台", () => Run(() =>
                    PathMechanismAuthoringService.CreateBoundPlatform())));
                body.Add(createRow);

                selectionHint = new Label("2. 选中对象后，这里会出现续接与绑定操作");
                selectionHint.style.whiteSpace = WhiteSpace.Normal;
                selectionHint.style.marginTop = 7f;
                selectionHint.style.unityFontStyleAndWeight = FontStyle.Bold;
                body.Add(selectionHint);

                ropeActions = Row();
                ropeActions.Add(Button("从 A 端继续", () => Run(() =>
                    PathMechanismAuthoringService.ContinueRope(RopeEndpoint.A))));
                ropeActions.Add(Button("从 B 端继续", () => Run(() =>
                    PathMechanismAuthoringService.ContinueRope(RopeEndpoint.B))));
                body.Add(ropeActions);

                ladderActions = Row();
                ladderActions.Add(Button("向下续梯子", () => Run(() =>
                    PathMechanismAuthoringService.ContinueLadder(false))));
                ladderActions.Add(Button("向上续梯子", () => Run(() =>
                    PathMechanismAuthoringService.ContinueLadder(true))));
                body.Add(ladderActions);

                platformActions = Row();
                platformActions.Add(Button("绑定到最近绳端", () => Run(() =>
                {
                    if (!PathMechanismAuthoringService
                            .BindSelectedPlatformToNearestEndpoint())
                    {
                        EditorUtility.DisplayDialog(
                            "无法绑定",
                            "请先选中平台，并确保场景中至少有一条绳子。",
                            "确定");
                    }
                })));
                platformActions.Add(Button("解除绑定", () => Run(ClearSelectedPlatform)));
                body.Add(platformActions);

                Foldout advanced = new Foldout
                {
                    text = "高级预览与吸附",
                    value = false
                };
                direction = new EnumField("2D 视角方向", RopeProjectionDirection.Front);
                direction.RegisterValueChangedCallback(evt =>
                    PathMechanismAuthoringService.SetPreviewDirection(
                        (RopeProjectionDirection)evt.newValue));
                advanced.Add(direction);

                ropeTolerance = new FloatField("绳子接续容差") { isDelayed = true };
                ropeTolerance.RegisterValueChangedCallback(evt =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    if (network == null)
                    {
                        return;
                    }

                    Undo.RecordObject(network, "修改绳子接续容差");
                    network.ConnectionTolerance = evt.newValue;
                    EditorUtility.SetDirty(network);
                });
                advanced.Add(ropeTolerance);

                ladderTolerance = new FloatField("梯子接续容差") { isDelayed = true };
                ladderTolerance.RegisterValueChangedCallback(evt =>
                {
                    LadderPathNetwork network =
                        PathMechanismAuthoringService.FindActiveLadderNetwork();
                    if (network == null)
                    {
                        return;
                    }

                    Undo.RecordObject(network, "修改梯子接续容差");
                    network.ConnectionTolerance = evt.newValue;
                    EditorUtility.SetDirty(network);
                });
                advanced.Add(ladderTolerance);

                VisualElement snapRow = Row();
                snapRow.Add(Button("投影吸附绳端", () => Run(() => SnapRope(true))));
                snapRow.Add(Button("实体吸附绳端", () => Run(() => SnapRope(false))));
                advanced.Add(snapRow);
                body.Add(advanced);

                Button repair = Button("检查并修复当前场景", () => Run(() =>
                    ShowRepairResult(
                        PathMechanismAuthoringService.RepairActiveScene())));
                repair.style.height = 30f;
                repair.style.marginTop = 7f;
                body.Add(repair);

                Label help = new Label(
                    "创建位置：优先使用当前选中对象的位置，否则使用 Scene 视图中心。\n" +
                    "平台会自动创建绳网、自动绑定最近端点并补齐碰撞体；不需要手工拖引用。\n" +
                    "保存当前 Scene 即可。每个人可以在自己的 Scene 中独立制作。" );
                help.style.whiteSpace = WhiteSpace.Normal;
                help.style.fontSize = 10f;
                help.style.opacity = 0.7f;
                help.style.marginTop = 6f;
                body.Add(help);
            }

            internal void Refresh()
            {
                RopePathNetwork ropeNetwork = RopePathEditorService.FindActiveNetwork();
                LadderPathNetwork ladderNetwork =
                    PathMechanismAuthoringService.FindActiveLadderNetwork();
                int ropeCount = ropeNetwork != null ? ropeNetwork.Segments.Count : 0;
                int ladderCount = ladderNetwork != null ? ladderNetwork.Segments.Count : 0;
                int platformCount = ropeNetwork != null
                    ? ropeNetwork.GetComponentsInChildren<RopePlatform>(true).Length
                    : 0;
                status.text =
                    $"当前场景：绳子 {ropeCount} · 梯子 {ladderCount} · 平台 {platformCount}";

                RopeSegment rope = PathMechanismAuthoringService.FindSelectedRope();
                LadderSegment ladder = PathMechanismAuthoringService.FindSelectedLadder();
                RopePlatform platform = PathMechanismAuthoringService.FindSelectedPlatform();
                ropeActions.style.display = rope != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                ladderActions.style.display = ladder != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                platformActions.style.display = platform != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                selectionHint.text = rope != null
                    ? "2. 已选中绳子：选择一端连续搭建"
                    : ladder != null
                        ? "2. 已选中梯子：向上或向下连续搭建"
                        : platform != null
                            ? platform.IsBound
                                ? "2. 已选中平台：当前已绑定"
                                : "2. 已选中平台：点击绑定到最近绳端"
                            : "2. 选中对象后，这里会出现续接与绑定操作";

                RopeProjectionDirection currentDirection = ropeNetwork != null
                    ? ropeNetwork.EditorDirection
                    : ladderNetwork != null
                        ? (RopeProjectionDirection)ladderNetwork.EditorDirection
                        : RopeProjectionDirection.Front;
                direction.SetValueWithoutNotify(currentDirection);
                ropeTolerance.SetValueWithoutNotify(
                    ropeNetwork != null ? ropeNetwork.ConnectionTolerance : 0.12f);
                ladderTolerance.SetValueWithoutNotify(
                    ladderNetwork != null ? ladderNetwork.ConnectionTolerance : 0.12f);
            }

            private static void ClearSelectedPlatform()
            {
                RopePlatform platform =
                    PathMechanismAuthoringService.FindSelectedPlatform();
                if (platform == null)
                {
                    return;
                }

                Undo.RecordObject(platform, "解除平台绑定");
                platform.ClearBinding();
                EditorUtility.SetDirty(platform);
                EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
            }

            private static void SnapRope(bool preserveDepth)
            {
                RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                bool success = preserveDepth
                    ? RopePathEditorService.SnapSelectedEndpoint(
                        network,
                        network != null
                            ? network.EditorDirection
                            : RopeProjectionDirection.Front,
                        RopePathEditorState.ActiveEndpoint)
                    : RopePathEditorService.SnapSelectedEndpointWorld(
                        network,
                        network != null
                            ? network.EditorDirection
                            : RopeProjectionDirection.Front,
                        RopePathEditorState.ActiveEndpoint);
                if (!success)
                {
                    EditorUtility.DisplayDialog(
                        "无法吸附",
                        "请先选中绳子端点，并让目标端点进入接续容差附近。",
                        "确定");
                }
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
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.paddingTop = 5f;
                label.style.paddingBottom = 5f;
                label.style.marginTop = 6f;
                label.style.backgroundColor = new Color(0.18f, 0.62f, 0.95f, 0.24f);
                return label;
            }

            private static Button BigButton(string text, Action action)
            {
                Button button = Button(text, action);
                button.style.height = 36f;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
                button.style.backgroundColor = new Color(0.16f, 0.5f, 0.86f, 1f);
                button.style.color = Color.white;
                return button;
            }

            private static Button Button(string text, Action action)
            {
                Button button = new Button(action) { text = text };
                button.style.flexGrow = 1f;
                button.style.height = 25f;
                button.style.marginLeft = 2f;
                button.style.marginRight = 2f;
                button.style.marginTop = 2f;
                button.style.marginBottom = 2f;
                return button;
            }

            private static Button SmallButton(string text, Action action)
            {
                Button button = action != null
                    ? new Button(action)
                    : new Button();
                button.text = text;
                button.style.width = 25f;
                button.style.height = 22f;
                button.style.marginLeft = 3f;
                return button;
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
