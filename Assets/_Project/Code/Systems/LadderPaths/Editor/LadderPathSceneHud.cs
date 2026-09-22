using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.LadderPaths.Editor
{
    [InitializeOnLoad]
    internal static class LadderPathSceneHud
    {
        private const string RootName = "ladder-path-scene-hud";

        static LadderPathSceneHud()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= RemoveAll;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveAll;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            VisualElement root = sceneView.rootVisualElement.Q<VisualElement>(RootName);
            if (root == null || !(root.userData is HudElements))
            {
                root?.RemoveFromHierarchy();
                root = new VisualElement { name = RootName };
                root.pickingMode = PickingMode.Ignore;
                root.style.position = Position.Absolute;
                root.style.right = 18f;
                root.style.top = 46f;
                root.userData = new HudElements(root);
                sceneView.rootVisualElement.Add(root);
            }

            ((HudElements)root.userData).Refresh();
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
            private readonly Label status;
            private readonly Label graph;
            private readonly EnumField direction;
            private readonly EnumField endpoint;
            private readonly FloatField tolerance;
            private readonly Button createNetwork;
            private readonly Button createLadder;
            private readonly Button projectionSnap;
            private readonly Button worldSnap;

            internal HudElements(VisualElement root)
            {
                VisualElement panel = new VisualElement();
                panel.pickingMode = PickingMode.Position;
                panel.style.width = 300f;
                panel.style.paddingLeft = 9f;
                panel.style.paddingRight = 9f;
                panel.style.paddingTop = 8f;
                panel.style.paddingBottom = 8f;
                panel.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.08f, 0.15f, 0.13f, 0.97f)
                    : new Color(0.91f, 0.97f, 0.94f, 0.98f);
                root.Add(panel);

                VisualElement header = Row();
                VisualElement titleGroup = new VisualElement();
                titleGroup.style.flexGrow = 1f;
                Label title = new Label("梯子路径编辑");
                title.style.fontSize = 13f;
                title.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleGroup.Add(title);
                Label subtitle = new Label("同面族投影接续 · 攀爬状态接口");
                subtitle.style.fontSize = 9f;
                subtitle.style.opacity = 0.65f;
                titleGroup.Add(subtitle);
                header.Add(titleGroup);
                VisualElement body = new VisualElement();
                Button collapse = new Button { text = "▾" };
                collapse.clicked += () =>
                {
                    bool hidden = body.style.display.value == DisplayStyle.None;
                    body.style.display = hidden ? DisplayStyle.Flex : DisplayStyle.None;
                    collapse.text = hidden ? "▾" : "▸";
                };
                collapse.style.width = 28f;
                collapse.style.height = 24f;
                header.Add(collapse);
                panel.Add(header);
                panel.Add(body);
                MakeDraggable(panel, header);

                status = new Label();
                status.style.unityTextAlign = TextAnchor.MiddleCenter;
                status.style.paddingTop = 4f;
                status.style.paddingBottom = 4f;
                status.style.backgroundColor = new Color(0.15f, 0.8f, 0.55f, 0.25f);
                body.Add(status);

                direction = new EnumField("当前投影", LadderProjectionDirection.Front);
                direction.RegisterValueChangedCallback(evt =>
                {
                    LadderPathNetwork network = LadderPathEditorService.FindActiveNetwork();
                    if (network == null)
                    {
                        return;
                    }

                    network.EditorDirection = (LadderProjectionDirection)evt.newValue;
                    EditorUtility.SetDirty(network);
                    SceneView.RepaintAll();
                });
                body.Add(direction);

                tolerance = new FloatField("接续容差") { isDelayed = true };
                tolerance.RegisterValueChangedCallback(evt =>
                {
                    LadderPathNetwork network = LadderPathEditorService.FindActiveNetwork();
                    if (network == null)
                    {
                        return;
                    }

                    Undo.RecordObject(network, "修改梯子接续容差");
                    network.ConnectionTolerance = evt.newValue;
                    EditorUtility.SetDirty(network);
                    SceneView.RepaintAll();
                });
                body.Add(tolerance);

                VisualElement createRow = Row();
                createNetwork = Button("创建网络", () =>
                {
                    LadderPathEditorService.CreateNetwork();
                    SceneView.RepaintAll();
                });
                createLadder = Button("＋ 梯子", () =>
                {
                    LadderPathEditorService.CreateSegment(
                        LadderPathEditorService.FindActiveNetwork());
                    SceneView.RepaintAll();
                });
                createRow.Add(createNetwork);
                createRow.Add(createLadder);
                body.Add(createRow);

                endpoint = new EnumField("吸附逻辑端", LadderEndpoint.Top);
                endpoint.RegisterValueChangedCallback(evt =>
                    LadderPathEditorState.ActiveEndpoint = (LadderEndpoint)evt.newValue);
                body.Add(endpoint);

                projectionSnap = Button("投影吸附", () => Snap(true));
                worldSnap = Button("实体吸附", () => Snap(false));
                VisualElement snapRow = Row();
                snapRow.Add(projectionSnap);
                snapRow.Add(worldSnap);
                body.Add(snapRow);

                body.Add(Button("重建四方向缓存", () =>
                {
                    LadderPathNetwork network = LadderPathEditorService.FindActiveNetwork();
                    network?.InvalidateCache();
                    SceneView.RepaintAll();
                }));

                graph = new Label();
                graph.style.whiteSpace = WhiteSpace.Normal;
                graph.style.fontSize = 10f;
                graph.style.marginTop = 5f;
                body.Add(graph);

                Label help = new Label(
                    "青绿色虚线为梯子逻辑端外延；亮绿色虚线、箭头和圆环为当前视角下的3D接续。\n" +
                    "正面族只接正面族，侧面族只接侧面族；白膜模型没有实体端点。" );
                help.style.whiteSpace = WhiteSpace.Normal;
                help.style.fontSize = 10f;
                help.style.opacity = 0.7f;
                body.Add(help);
            }

            internal void Refresh()
            {
                LadderPathNetwork network = LadderPathEditorService.FindActiveNetwork();
                bool hasNetwork = network != null;
                createNetwork.SetEnabled(!hasNetwork);
                createLadder.SetEnabled(hasNetwork);
                bool hasSegment = LadderPathEditorService.FindSelectedSegment() != null;
                projectionSnap.SetEnabled(hasNetwork && hasSegment);
                worldSnap.SetEnabled(hasNetwork && hasSegment);
                direction.SetEnabled(hasNetwork);
                tolerance.SetEnabled(hasNetwork);
                endpoint.SetValueWithoutNotify(LadderPathEditorState.ActiveEndpoint);

                if (!hasNetwork)
                {
                    status.text = "当前场景未创建梯子路径网络";
                    graph.text = "先创建网络，再添加梯子。";
                    return;
                }

                direction.SetValueWithoutNotify(network.EditorDirection);
                tolerance.SetValueWithoutNotify(network.ConnectionTolerance);
                LadderPathGraph path = network.BuildPath(network.EditorDirection);
                status.text = $"网络：{network.name} · 梯子 {network.Segments.Count}";
                graph.text =
                    $"{network.EditorDirection}：接续 {path.Connections.Count} 条\n" +
                    "玩家控制器通过 ILadderClimbStateReceiver 接收攀爬状态。";
            }

            private static void Snap(bool preserveDepth)
            {
                LadderPathNetwork network = LadderPathEditorService.FindActiveNetwork();
                if (network == null || !LadderPathEditorService.SnapSelectedEndpoint(
                        network,
                        network.EditorDirection,
                        LadderPathEditorState.ActiveEndpoint,
                        preserveDepth))
                {
                    EditorUtility.DisplayDialog(
                        "无法吸附梯子",
                        "请选择梯子，并确保同面族的相反逻辑端在接续容差附近。",
                        "确定");
                }
            }

            private static VisualElement Row()
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                return row;
            }

            private static void MakeDraggable(
                VisualElement panel,
                VisualElement header)
            {
                bool dragging = false;
                int pointerId = -1;
                Vector2 lastPointer = Vector2.zero;

                header.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button != 0 ||
                        evt.target is Button ||
                        dragging)
                    {
                        return;
                    }

                    Rect layout = panel.layout;
                    panel.style.left = layout.x;
                    panel.style.top = layout.y;
                    panel.style.right = StyleKeyword.Auto;
                    panel.style.bottom = StyleKeyword.Auto;
                    dragging = true;
                    pointerId = evt.pointerId;
                    lastPointer = new Vector2(evt.position.x, evt.position.y);
                    header.CapturePointer(pointerId);
                    evt.StopPropagation();
                });

                header.RegisterCallback<PointerMoveEvent>(evt =>
                {
                    if (!dragging || evt.pointerId != pointerId)
                    {
                        return;
                    }

                    Vector2 currentPointer = new Vector2(
                        evt.position.x,
                        evt.position.y);
                    Vector2 delta = currentPointer - lastPointer;
                    lastPointer = currentPointer;
                    panel.style.left = panel.layout.x + delta.x;
                    panel.style.top = panel.layout.y + delta.y;
                    evt.StopPropagation();
                });

                header.RegisterCallback<PointerUpEvent>(evt =>
                {
                    if (!dragging || evt.pointerId != pointerId)
                    {
                        return;
                    }

                    dragging = false;
                    header.ReleasePointer(pointerId);
                    pointerId = -1;
                    evt.StopPropagation();
                });

                header.RegisterCallback<PointerCaptureOutEvent>(evt =>
                {
                    if (evt.pointerId == pointerId)
                    {
                        dragging = false;
                        pointerId = -1;
                    }
                });
            }

            private static Button Button(string text, Action action)
            {
                Button button = new Button(action) { text = text };
                button.style.flexGrow = 1f;
                button.style.height = 24f;
                button.style.marginLeft = 2f;
                button.style.marginRight = 2f;
                button.style.marginTop = 2f;
                button.style.marginBottom = 2f;
                return button;
            }
        }
    }
}
