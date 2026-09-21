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
                AddTitle(navigationPanel, "立方体地图编辑", "多场景协作工作区");
                sceneBadge = CreateBadge("未初始化");
                navigationPanel.Add(sceneBadge);
                initializeButton = CreateButton(
                    "初始化 LV001 地图工作区",
                    () => RunDeferred(CubeMapWorkspaceService.InitializeWorkspace),
                    true);
                navigationPanel.Add(initializeButton);
                VisualElement navigationRow = CreateRow();
                total2DButton = CreateButton(
                    "打开 2D 总拼",
                    () => RunDeferred(CubeMapWorkspaceService.OpenTotal2D));
                main3DButton = CreateButton(
                    "打开 3D 主场景",
                    () => RunDeferred(CubeMapWorkspaceService.OpenMain3D));
                navigationRow.Add(total2DButton);
                navigationRow.Add(main3DButton);
                navigationPanel.Add(navigationRow);
                settingsButton = CreateButton(
                    "工作区参数",
                    () => RunDeferred(CubeMapWorkspaceService.SelectWorkspaceAsset));
                navigationPanel.Add(settingsButton);

                piecePanel = CreatePanel(root, panelColor, null, 46f, 12f, null, 250f);
                AddTitle(piecePanel, "关卡小拼图", "每关独立 Scene，可分给不同策划");
                pieceLabel = new Label("第 00 / 00 关");
                pieceLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                pieceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                pieceLabel.style.marginBottom = 4f;
                piecePanel.Add(pieceLabel);
                VisualElement pieceRow = CreateRow();
                previousPieceButton = CreateButton("◀", PreviousPiece);
                openPieceButton = CreateButton(
                    "打开小拼图",
                    () => RunDeferred(() =>
                        CubeMapWorkspaceService.OpenPiece(SelectedPieceIndex)),
                    true);
                nextPieceButton = CreateButton("▶", NextPiece);
                pieceRow.Add(previousPieceButton);
                pieceRow.Add(openPieceButton);
                pieceRow.Add(nextPieceButton);
                piecePanel.Add(pieceRow);
                addPieceButton = CreateButton(
                    "＋ 新建下一关",
                    () => RunDeferred(CubeMapWorkspaceService.AddPieceAndOpen));
                piecePanel.Add(addPieceButton);

                guidePanel = CreatePanel(root, panelColor, 12f, null, null, 12f, 258f);
                AddTitle(guidePanel, "场景辅助", "色块对应四面，虚线表示分格");
                guidesToggle = new Toggle("显示分面色块与网格")
                {
                    value = GuidesVisible
                };
                guidesToggle.RegisterValueChangedCallback(evt =>
                {
                    GuidesVisible = evt.newValue;
                    SceneView.RepaintAll();
                });
                guidePanel.Add(guidesToggle);
                dimensionsLabel = new Label();
                dimensionsLabel.style.fontSize = 10f;
                dimensionsLabel.style.opacity = 0.68f;
                dimensionsLabel.style.marginTop = 3f;
                guidePanel.Add(dimensionsLabel);

                buildPanel = CreatePanel(root, panelColor, null, null, 12f, 12f, 276f);
                AddTitle(buildPanel, "生成与拼合", "生成场景可反复覆盖，源小拼图不受影响");
                rebuild2DButton = CreateButton(
                    "刷新 2D 总拼预览",
                    () => RunDeferred(CubeMapWorkspaceService.BuildTotal2D));
                buildPanel.Add(rebuild2DButton);
                build3DButton = CreateButton(
                    "拼合并打开 3D 主场景",
                    () => RunDeferred(CubeMapWorkspaceService.BuildMain3D),
                    true);
                build3DButton.style.height = 32f;
                build3DButton.style.backgroundColor = new Color(0.18f, 0.52f, 0.92f, 1f);
                build3DButton.style.color = Color.white;
                buildPanel.Add(build3DButton);
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

                CubeMapWorkspaceDefinition workspace = CubeMapWorkspaceService.LoadWorkspace();
                bool ready = workspace != null;
                initializeButton.style.display = ready ? DisplayStyle.None : DisplayStyle.Flex;
                total2DButton.SetEnabled(ready);
                main3DButton.SetEnabled(ready);
                settingsButton.SetEnabled(ready);
                piecePanel.SetEnabled(ready);
                guidePanel.SetEnabled(ready);
                buildPanel.SetEnabled(ready);

                if (!ready)
                {
                    sceneBadge.text = "未初始化";
                    pieceLabel.text = "请先初始化工作区";
                    dimensionsLabel.text = "初始化后自动创建 4 个独立关卡 Scene";
                    return;
                }

                int pieceCount = workspace.PieceCount;
                SelectedPieceIndex = pieceCount > 0
                    ? Mathf.Clamp(SelectedPieceIndex, 0, pieceCount - 1)
                    : 0;
                previousPieceButton.SetEnabled(pieceCount > 1);
                nextPieceButton.SetEnabled(pieceCount > 1);
                openPieceButton.SetEnabled(pieceCount > 0);
                addPieceButton.SetEnabled(true);
                pieceLabel.text = pieceCount > 0
                    ? $"第 {SelectedPieceIndex + 1:00} / {pieceCount:00} 关"
                    : "暂无小拼图";
                dimensionsLabel.text =
                    $"单面宽 {workspace.FaceWidth:0.#} · 单关高 {workspace.PieceHeight:0.#} · 共 {pieceCount} 关";
                sceneBadge.text = ResolveSceneBadge(workspace);
                guidesToggle.SetValueWithoutNotify(GuidesVisible);
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

            private static void AddTitle(VisualElement panel, string title, string subtitle)
            {
                Label titleLabel = new Label(title);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                titleLabel.style.fontSize = 13f;
                panel.Add(titleLabel);

                Label subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 9f;
                subtitleLabel.style.opacity = 0.62f;
                subtitleLabel.style.marginBottom = 5f;
                panel.Add(subtitleLabel);
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
