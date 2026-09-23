using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Project.CubeMapEditing.Editor
{
    [InitializeOnLoad]
    internal static class CubeMapSceneHud
    {
        private const string RootName = "cube-map-scene-hud";
        private const string GuidesPreference = "2026Test.CubeMap.GuidesVisible";
        private const string PiecePreference = "2026Test.CubeMap.SelectedPiece";

        static CubeMapSceneHud()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= RemoveAll;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveAll;
        }

        internal static bool GuidesVisible
        {
            get => EditorPrefs.GetBool(GuidesPreference, true);
            set => EditorPrefs.SetBool(GuidesPreference, value);
        }

        private static int SelectedPieceIndex
        {
            get => EditorPrefs.GetInt(PiecePreference, 0);
            set => EditorPrefs.SetInt(PiecePreference, Mathf.Max(0, value));
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            VisualElement root = sceneView.rootVisualElement.Q<VisualElement>(RootName);
            if (root == null || !(root.userData is HudElements))
            {
                root?.RemoveFromHierarchy();
                root = CreateHud();
                sceneView.rootVisualElement.Add(root);
            }

            ((HudElements)root.userData).Refresh();
            CubeMapGuideRenderer.DrawActiveScene();
        }

        private static VisualElement CreateHud()
        {
            VisualElement root = new VisualElement { name = RootName };
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.right = 0f;
            root.style.top = 0f;
            root.style.bottom = 0f;

            HudElements elements = new HudElements(root);
            root.userData = elements;
            return root;
        }

        private static void RemoveAll()
        {
            foreach (SceneView view in SceneView.sceneViews)
            {
                view.rootVisualElement.Q<VisualElement>(RootName)?.RemoveFromHierarchy();
            }
        }

        private static void RunDeferred(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                action();
                SceneView.RepaintAll();
            };
        }

        private sealed class HudElements
        {
            private readonly VisualElement navigationPanel;
            private readonly VisualElement piecePanel;
            private readonly VisualElement guidePanel;
            private readonly VisualElement buildPanel;
            private readonly VisualElement toolModePanel;
            private readonly Button assemblyModeButton;
            private readonly Button gridModeButton;
            private readonly Label sceneBadge;
            private readonly Label pieceLabel;
            private readonly Label dimensionsLabel;
            private readonly Button initializeButton;
            private readonly Button previousPieceButton;
            private readonly Button openPieceButton;
            private readonly Button nextPieceButton;
            private readonly Button addPieceButton;
            private readonly Button total2DButton;
            private readonly Button main3DButton;
            private readonly Button rebuild2DButton;
            private readonly Button build3DButton;
            private readonly Button settingsButton;
            private readonly Toggle guidesToggle;

            internal HudElements(VisualElement root)
            {
                Color panelColor = EditorGUIUtility.isProSkin
                    ? new Color(0.12f, 0.13f, 0.15f, 0.94f)
                    : new Color(0.93f, 0.94f, 0.96f, 0.96f);

                toolModePanel = new VisualElement();
                toolModePanel.pickingMode = PickingMode.Position;
                toolModePanel.style.position = Position.Absolute;
                toolModePanel.style.top = 12f;
                toolModePanel.style.left = Length.Percent(50f);
                toolModePanel.style.translate = new Translate(-110f, 0f, 0f);
                toolModePanel.style.flexDirection = FlexDirection.Row;
                toolModePanel.style.paddingLeft = 4f;
                toolModePanel.style.paddingRight = 4f;
                toolModePanel.style.paddingTop = 3f;
                toolModePanel.style.paddingBottom = 3f;
                toolModePanel.style.backgroundColor = panelColor;
                toolModePanel.style.borderTopLeftRadius = 6f;
                toolModePanel.style.borderTopRightRadius = 6f;
                toolModePanel.style.borderBottomLeftRadius = 6f;
                toolModePanel.style.borderBottomRightRadius = 6f;
                assemblyModeButton = CreateButton("拼合预览", () =>
                    CubeMapEditorToolState.ActiveMode = CubeMapEditorToolMode.Assembly, true);
                gridModeButton = CreateButton("网格地图", () =>
                    CubeMapEditorToolState.ActiveMode = CubeMapEditorToolMode.GridMap, true);
                assemblyModeButton.style.width = 96f;
                gridModeButton.style.width = 96f;
                toolModePanel.Add(assemblyModeButton);
                toolModePanel.Add(gridModeButton);
                root.Add(toolModePanel);

                navigationPanel = CreatePanel(root, panelColor, 12f, 46f, null, null, 258f);
                VisualElement navigationBody = AddTitle(
                    navigationPanel,
                    "关卡创作管线",
                    "数据块是唯一源，Scene 只做预览");
                sceneBadge = CreateBadge("未初始化");
                navigationBody.Add(sceneBadge);
                initializeButton = CreateButton(
                    "打开新关卡管线窗口",
                    OpenLevelAuthoringPipeline,
                    true);
                navigationBody.Add(initializeButton);
                VisualElement navigationRow = CreateRow();
                total2DButton = CreateButton(
                    "管理 2D 预览",
                    OpenLevelAuthoringPipeline);
                main3DButton = CreateButton(
                    "管理 3D 预览",
                    OpenLevelAuthoringPipeline);
                navigationRow.Add(total2DButton);
                navigationRow.Add(main3DButton);
                navigationBody.Add(navigationRow);
                settingsButton = CreateButton(
                    "布局与数据块",
                    OpenLevelAuthoringPipeline);
                navigationBody.Add(settingsButton);

                piecePanel = CreatePanel(root, panelColor, null, 46f, 12f, null, 250f);
                VisualElement pieceBody = AddTitle(
                    piecePanel,
                    "关卡小拼图",
                    "每关独立 Scene，可分给不同策划");
                pieceLabel = new Label("第 00 / 00 关");
                pieceLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                pieceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                pieceLabel.style.marginBottom = 4f;
                pieceBody.Add(pieceLabel);
                VisualElement pieceRow = CreateRow();
                previousPieceButton = CreateButton("◀", PreviousPiece);
                openPieceButton = CreateButton(
                    "打开管线选择 Piece",
                    OpenLevelAuthoringPipeline,
                    true);
                nextPieceButton = CreateButton("▶", NextPiece);
                pieceRow.Add(previousPieceButton);
                pieceRow.Add(openPieceButton);
                pieceRow.Add(nextPieceButton);
                pieceBody.Add(pieceRow);
                addPieceButton = CreateButton(
                    "Piece 数量由管线定义管理",
                    OpenLevelAuthoringPipeline);
                pieceBody.Add(addPieceButton);

                guidePanel = CreatePanel(root, panelColor, 12f, null, null, 12f, 258f);
                VisualElement guideBody = AddTitle(
                    guidePanel,
                    "场景辅助",
                    "色块对应四面，虚线表示分格");
                guidesToggle = new Toggle("显示分面色块与网格")
                {
                    value = GuidesVisible
                };
                guidesToggle.RegisterValueChangedCallback(evt =>
                {
                    GuidesVisible = evt.newValue;
                    SceneView.RepaintAll();
                });
                guideBody.Add(guidesToggle);
                dimensionsLabel = new Label();
                dimensionsLabel.style.fontSize = 10f;
                dimensionsLabel.style.opacity = 0.68f;
                dimensionsLabel.style.marginTop = 3f;
                guideBody.Add(dimensionsLabel);

                buildPanel = CreatePanel(root, panelColor, null, null, 12f, 12f, 276f);
                VisualElement buildBody = AddTitle(
                    buildPanel,
                    "生成与拼合",
                    "生成场景可反复覆盖，源小拼图不受影响");
                rebuild2DButton = CreateButton(
                    "在管线窗口重建全部预览",
                    OpenLevelAuthoringPipeline);
                buildBody.Add(rebuild2DButton);
                build3DButton = CreateButton(
                    "在管线窗口验证并发布",
                    OpenLevelAuthoringPipeline,
                    true);
                build3DButton.style.height = 32f;
                build3DButton.style.backgroundColor = new Color(0.18f, 0.52f, 0.92f, 1f);
                build3DButton.style.color = Color.white;
                buildBody.Add(build3DButton);
            }

            internal void Refresh()
            {
                bool assemblyMode = CubeMapEditorToolState.IsAssembly;
                navigationPanel.style.display = assemblyMode ? DisplayStyle.Flex : DisplayStyle.None;
                piecePanel.style.display = assemblyMode ? DisplayStyle.Flex : DisplayStyle.None;
                guidePanel.style.display = assemblyMode ? DisplayStyle.Flex : DisplayStyle.None;
                buildPanel.style.display = assemblyMode ? DisplayStyle.Flex : DisplayStyle.None;
                assemblyModeButton.style.backgroundColor = assemblyMode
                    ? new Color(0.18f, 0.52f, 0.92f, 1f)
                    : StyleKeyword.Null;
                gridModeButton.style.backgroundColor = assemblyMode
                    ? StyleKeyword.Null
                    : new Color(0.18f, 0.52f, 0.92f, 1f);
                assemblyModeButton.style.color = assemblyMode ? Color.white : StyleKeyword.Null;
                gridModeButton.style.color = assemblyMode ? StyleKeyword.Null : Color.white;

                CubeMapPieceAuthoring activePiece =
                    UnityEngine.Object.FindFirstObjectByType<CubeMapPieceAuthoring>();
                CubeMapWorkspaceDefinition workspace = activePiece != null
                    ? activePiece.Workspace
                    : null;
                bool ready = workspace != null;
                initializeButton.style.display = DisplayStyle.Flex;
                total2DButton.SetEnabled(true);
                main3DButton.SetEnabled(true);
                settingsButton.SetEnabled(true);
                piecePanel.SetEnabled(true);
                guidePanel.SetEnabled(ready);
                buildPanel.SetEnabled(true);

                if (!ready)
                {
                    sceneBadge.text = "当前不是 Piece 预览";
                    pieceLabel.text = "请从新管线生成并打开 Piece";
                    dimensionsLabel.text = "旧场景入口已停用；不会再创建 Scenes/Levels/LV001";
                    previousPieceButton.SetEnabled(false);
                    nextPieceButton.SetEnabled(false);
                    return;
                }

                previousPieceButton.SetEnabled(false);
                nextPieceButton.SetEnabled(false);
                openPieceButton.SetEnabled(true);
                addPieceButton.SetEnabled(true);
                pieceLabel.text = $"当前 Piece {activePiece.PieceIndex:00}";
                dimensionsLabel.text =
                    $"单面宽 {workspace.FaceWidth:0.#} · 单 Piece 高 {workspace.PieceHeight:0.#}";
                sceneBadge.text = "新框架 Piece 预览";
                guidesToggle.SetValueWithoutNotify(GuidesVisible);
            }

            private static void OpenLevelAuthoringPipeline()
            {
                EditorApplication.ExecuteMenuItem(
                    "Tools/2026Test/关卡创作管线/打开管线窗口");
            }

            private static string ResolveSceneBadge(CubeMapWorkspaceDefinition workspace)
            {
                string path = SceneManager.GetActiveScene().path;
                if (path == workspace.Total2DScenePath)
                {
                    return "2D 总拼预览";
                }

                if (path == workspace.Main3DScenePath)
                {
                    return "3D 主场景";
                }

                for (int index = 0; index < workspace.PieceCount; index++)
                {
                    if (path == workspace.PieceScenePaths[index])
                    {
                        return $"小拼图 {index + 1:00}";
                    }
                }

                return "其他场景";
            }

            private void PreviousPiece()
            {
                CubeMapWorkspaceDefinition workspace = CubeMapWorkspaceService.LoadWorkspace();
                if (workspace == null || workspace.PieceCount == 0)
                {
                    return;
                }

                SelectedPieceIndex =
                    (SelectedPieceIndex - 1 + workspace.PieceCount) % workspace.PieceCount;
                SceneView.RepaintAll();
            }

            private void NextPiece()
            {
                CubeMapWorkspaceDefinition workspace = CubeMapWorkspaceService.LoadWorkspace();
                if (workspace == null || workspace.PieceCount == 0)
                {
                    return;
                }

                SelectedPieceIndex = (SelectedPieceIndex + 1) % workspace.PieceCount;
                SceneView.RepaintAll();
            }

            private static VisualElement CreatePanel(
                VisualElement root,
                Color background,
                float? left,
                float? top,
                float? right,
                float? bottom,
                float width)
            {
                VisualElement panel = new VisualElement();
                panel.pickingMode = PickingMode.Position;
                panel.style.position = Position.Absolute;
                if (left.HasValue)
                {
                    panel.style.left = left.Value;
                }

                if (top.HasValue)
                {
                    panel.style.top = top.Value;
                }

                if (right.HasValue)
                {
                    panel.style.right = right.Value;
                }

                if (bottom.HasValue)
                {
                    panel.style.bottom = bottom.Value;
                }

                panel.style.width = width;
                panel.style.backgroundColor = background;
                panel.style.paddingLeft = 9f;
                panel.style.paddingRight = 9f;
                panel.style.paddingTop = 8f;
                panel.style.paddingBottom = 8f;
                panel.style.borderTopLeftRadius = 7f;
                panel.style.borderTopRightRadius = 7f;
                panel.style.borderBottomLeftRadius = 7f;
                panel.style.borderBottomRightRadius = 7f;
                root.Add(panel);
                return panel;
            }

            private static VisualElement AddTitle(
                VisualElement panel,
                string title,
                string subtitle)
            {
                VisualElement header = CreateRow();
                header.style.alignItems = Align.FlexStart;

                VisualElement titleGroup = new VisualElement();
                titleGroup.style.flexGrow = 1f;
                Label titleLabel = new Label(title);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.fontSize = 13f;
                titleGroup.Add(titleLabel);

                Label subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 9f;
                subtitleLabel.style.opacity = 0.62f;
                subtitleLabel.style.marginBottom = 5f;
                titleGroup.Add(subtitleLabel);
                header.Add(titleGroup);

                VisualElement body = new VisualElement();
                Button collapseButton = CreateCollapseButton(body);
                header.Add(collapseButton);
                panel.Add(header);
                panel.Add(body);
                MakePanelInteractive(panel, header);
                return body;
            }

            private static Button CreateCollapseButton(
                VisualElement body)
            {
                Button button = new Button
                {
                    text = "▾"
                };
                button.style.width = 24f;
                button.style.height = 20f;
                button.style.marginLeft = 4f;
                button.style.marginTop = 0f;
                button.style.marginRight = 0f;
                button.style.marginBottom = 0f;
                button.style.backgroundColor =
                    new Color(0.18f, 0.52f, 0.92f, 1f);
                button.style.color = Color.white;
                button.style.unityFontStyleAndWeight = FontStyle.Bold;
                button.clicked += () =>
                {
                    bool collapsed =
                        body.style.display.value == DisplayStyle.None;
                    body.style.display = collapsed
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                    button.text = collapsed ? "▾" : "▸";
                };
                return button;
            }

            private static void MakePanelInteractive(
                VisualElement panel,
                VisualElement header)
            {
                MakeDraggable(panel, header);
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
                    lastPointer = new Vector2(
                        evt.position.x,
                        evt.position.y);
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
                    Vector2 delta =
                        currentPointer - lastPointer;
                    lastPointer = currentPointer;
                    panel.style.left =
                        panel.layout.x + delta.x;
                    panel.style.top =
                        panel.layout.y + delta.y;
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


            private static Label CreateBadge(string text)
            {
                Label badge = new Label(text);
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.style.backgroundColor = new Color(0.18f, 0.52f, 0.92f, 0.26f);
                badge.style.paddingTop = 3f;
                badge.style.paddingBottom = 3f;
                badge.style.marginBottom = 5f;
                badge.style.borderTopLeftRadius = 5f;
                badge.style.borderTopRightRadius = 5f;
                badge.style.borderBottomLeftRadius = 5f;
                badge.style.borderBottomRightRadius = 5f;
                return badge;
            }

            private static Button CreateButton(
                string text,
                Action action,
                bool bold = false)
            {
                Button button = new Button(action) { text = text };
                button.style.flexGrow = 1f;
                button.style.height = 24f;
                button.style.marginLeft = 2f;
                button.style.marginRight = 2f;
                button.style.marginTop = 2f;
                button.style.marginBottom = 2f;
                if (bold)
                {
                    button.style.unityFontStyleAndWeight = FontStyle.Bold;
                }

                return button;
            }

            private static VisualElement CreateRow()
            {
                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                return row;
            }
        }
    }
}
