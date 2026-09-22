using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.RopePaths.Editor
{
    [InitializeOnLoad]
    internal static class RopePathSceneHud
    {
        private const string RootName = "rope-path-scene-hud";

        static RopePathSceneHud()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= RemoveAll;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveAll;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            VisualElement root = sceneView.rootVisualElement.Q<VisualElement>(RootName);
            if (root == null || !(root.userData is RopeHudElements))
            {
                root?.RemoveFromHierarchy();
                root = new VisualElement { name = RootName };
                root.pickingMode = PickingMode.Ignore;
                root.style.position = Position.Absolute;
                root.style.left = 280f;
                root.style.top = 46f;
                root.userData = new RopeHudElements(root);
                sceneView.rootVisualElement.Add(root);
            }

            ((RopeHudElements)root.userData).Refresh();
        }

        private static void RemoveAll()
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                sceneView.rootVisualElement.Q<VisualElement>(RootName)?.RemoveFromHierarchy();
            }
        }

        private sealed class RopeHudElements
        {
            private readonly VisualElement panel;
            private readonly Label statusLabel;
            private readonly Label graphLabel;
            private readonly EnumField directionField;
            private readonly EnumField endpointField;
            private readonly FloatField toleranceField;
            private readonly Button createNetworkButton;
            private readonly Button createRopeButton;
            private readonly Button createPlatformButton;
            private readonly Button snapButton;
            private readonly Button bindButton;
            private readonly Button unbindButton;

            internal RopeHudElements(VisualElement root)
            {
                panel = CreatePanel(root);
                AddTitle(panel, "绳子路径编辑", "四方向投影接续 · 端点吸附 · 平台绑定");
                statusLabel = CreateBadge("当前场景未创建绳子路径网络");
                panel.Add(statusLabel);

                directionField = new EnumField("当前投影", RopeProjectionDirection.Front);
                directionField.RegisterValueChangedCallback(evt =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    if (network == null)
                    {
                        return;
                    }

                    network.EditorDirection = (RopeProjectionDirection)evt.newValue;
                    EditorUtility.SetDirty(network);
                    SceneView.RepaintAll();
                });
                panel.Add(directionField);

                toleranceField = new FloatField("接续容差")
                {
                    isDelayed = true,
                    tooltip = "投影端点距离小于此值时视为同一个停靠点"
                };
                toleranceField.RegisterValueChangedCallback(evt =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    if (network == null)
                    {
                        return;
                    }

                    Undo.RecordObject(network, "修改绳子接续容差");
                    network.ConnectionTolerance = evt.newValue;
                    EditorUtility.SetDirty(network);
                    SceneView.RepaintAll();
                });
                panel.Add(toleranceField);

                VisualElement createRow = CreateRow();
                createNetworkButton = CreateButton("创建网络", () =>
                {
                    RopePathEditorService.CreateNetwork();
                    SceneView.RepaintAll();
                });
                createRopeButton = CreateButton("＋ 绳子", () =>
                {
                    RopePathEditorService.CreateSegment(RopePathEditorService.FindActiveNetwork());
                    SceneView.RepaintAll();
                });
                createPlatformButton = CreateButton("＋ 平台", () =>
                {
                    RopePathEditorService.CreatePlatform(RopePathEditorService.FindActiveNetwork());
                    SceneView.RepaintAll();
                });
                createRow.Add(createNetworkButton);
                createRow.Add(createRopeButton);
                createRow.Add(createPlatformButton);
                panel.Add(createRow);

                Foldout endpointFoldout = new Foldout
                {
                    text = "端点与平台辅助",
                    value = true
                };
                endpointField = new EnumField("编辑端点", RopeEndpoint.A);
                endpointField.RegisterValueChangedCallback(evt =>
                {
                    RopePathEditorState.ActiveEndpoint = (RopeEndpoint)evt.newValue;
                    SceneView.RepaintAll();
                });
                endpointFoldout.Add(endpointField);
                snapButton = CreateButton("投影吸附选中端点", () =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    if (!RopePathEditorService.SnapSelectedEndpoint(
                            network,
                            network != null ? network.EditorDirection : RopeProjectionDirection.Front,
                            RopePathEditorState.ActiveEndpoint))
                    {
                        EditorUtility.DisplayDialog(
                            "无法吸附端点",
                            "请选中绳子，并确保当前投影方向存在距离接续容差内的其他端点。",
                            "确定");
                    }
                });
                endpointFoldout.Add(snapButton);
                endpointFoldout.Add(CreateButton("实体吸附选中端点", () =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    if (!RopePathEditorService.SnapSelectedEndpointWorld(
                            network,
                            network != null ? network.EditorDirection : RopeProjectionDirection.Front,
                            RopePathEditorState.ActiveEndpoint))
                    {
                        EditorUtility.DisplayDialog(
                            "无法吸附端点",
                            "请选中绳子，并确保当前投影方向存在距离接续容差内的其他端点。",
                            "确定");
                    }
                }));
                bindButton = CreateButton("绑定选中平台到最近端点", () =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    if (!RopePathEditorService.BindSelectedPlatform(
                            network,
                            network != null ? network.EditorDirection : RopeProjectionDirection.Front))
                    {
                        EditorUtility.DisplayDialog(
                            "无法绑定平台",
                            "请选中平台，并把它放到当前投影方向端点附近。",
                            "确定");
                    }
                });
                endpointFoldout.Add(bindButton);
                unbindButton = CreateButton("解除选中平台绑定", () =>
                {
                    RopePlatform platform = RopePathEditorService.FindSelectedPlatform();
                    if (platform == null)
                    {
                        return;
                    }

                    Undo.RecordObject(platform, "解除绳索平台绑定");
                    platform.ClearBinding();
                    RopePathEditorService.MarkDirty(
                        RopePathEditorService.FindActiveNetwork(),
                        platform.gameObject);
                    SceneView.RepaintAll();
                });
                endpointFoldout.Add(unbindButton);
                panel.Add(endpointFoldout);

                panel.Add(CreateButton("重建四方向路径缓存", () =>
                {
                    RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                    network?.RebuildPaths();
                    SceneView.RepaintAll();
                }));

                graphLabel = new Label();
                graphLabel.style.whiteSpace = WhiteSpace.Normal;
                graphLabel.style.fontSize = 10f;
                graphLabel.style.marginTop = 4f;
                panel.Add(graphLabel);
                Label help = new Label(
                    "拖动绳子根部手柄可整体移动/旋转；点击 A/B 端点后可单独调整；\n" +
                    "3D 黄色虚线与箭头表示当前投影下已识别的空间接续。平台只保存绑定，不包含移动逻辑。");
                help.style.whiteSpace = WhiteSpace.Normal;
                help.style.fontSize = 10f;
                help.style.opacity = 0.68f;
                panel.Add(help);
            }

            internal void Refresh()
            {
                RopePathNetwork network = RopePathEditorService.FindActiveNetwork();
                bool hasNetwork = network != null;
                createNetworkButton.SetEnabled(!hasNetwork);
                createRopeButton.SetEnabled(hasNetwork);
                createPlatformButton.SetEnabled(hasNetwork);
                snapButton.SetEnabled(hasNetwork && RopePathEditorService.FindSelectedSegment() != null);
                RopePlatform selectedPlatform = RopePathEditorService.FindSelectedPlatform();
                bindButton.SetEnabled(hasNetwork && selectedPlatform != null);
                unbindButton.SetEnabled(selectedPlatform != null && selectedPlatform.IsBound);
                directionField.SetEnabled(hasNetwork);
                toleranceField.SetEnabled(hasNetwork);
                endpointField.SetValueWithoutNotify(RopePathEditorState.ActiveEndpoint);

                if (!hasNetwork)
                {
                    statusLabel.text = "当前场景未创建绳子路径网络";
                    graphLabel.text = "先创建网络，再添加绳子段和平台。";
                    return;
                }

                directionField.SetValueWithoutNotify(network.EditorDirection);
                toleranceField.SetValueWithoutNotify(network.ConnectionTolerance);
                RopePathGraph graph = network.BuildPath(network.EditorDirection);
                statusLabel.text = $"网络：{network.name} · 绳子 {network.Segments.Count}";
                graphLabel.text =
                    $"{network.EditorDirection} 投影：接续 {graph.Connections.Count} 条 · " +
                    $"路径 {graph.Paths.Count} 条\n" +
                    "路径数据通过 RopePathNetwork API 提供给移动平台系统。";
            }

            private static VisualElement CreatePanel(VisualElement root)
            {
                VisualElement value = new VisualElement();
                value.pickingMode = PickingMode.Position;
                value.style.width = 305f;
                value.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.1f, 0.12f, 0.15f, 0.97f)
                    : new Color(0.94f, 0.95f, 0.97f, 0.98f);
                value.style.paddingLeft = 9f;
                value.style.paddingRight = 9f;
                value.style.paddingTop = 8f;
                value.style.paddingBottom = 8f;
                value.style.borderTopLeftRadius = 7f;
                value.style.borderTopRightRadius = 7f;
                value.style.borderBottomLeftRadius = 7f;
                value.style.borderBottomRightRadius = 7f;
                root.Add(value);
                return value;
            }

            private static void AddTitle(VisualElement panel, string title, string subtitle)
            {
                Label titleLabel = new Label(title);
                titleLabel.style.fontSize = 13f;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                panel.Add(titleLabel);
                Label subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 9f;
                subtitleLabel.style.opacity = 0.65f;
                subtitleLabel.style.marginBottom = 5f;
                panel.Add(subtitleLabel);
            }

            private static Label CreateBadge(string text)
            {
                Label badge = new Label(text);
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.style.paddingTop = 4f;
                badge.style.paddingBottom = 4f;
                badge.style.backgroundColor = new Color(0.18f, 0.68f, 1f, 0.28f);
                return badge;
            }

            private static VisualElement CreateRow()
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                return row;
            }

            private static Button CreateButton(string text, Action action)
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
