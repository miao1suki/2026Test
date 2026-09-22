using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.CubeMapEditing.Editor
{
    [InitializeOnLoad]
    internal static class GridMapSceneHud
    {
        private const string RootName = "cube-map-grid-scene-hud";

        static GridMapSceneHud()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
            AssemblyReloadEvents.beforeAssemblyReload -= RemoveAll;
            AssemblyReloadEvents.beforeAssemblyReload += RemoveAll;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            VisualElement root = sceneView.rootVisualElement.Q<VisualElement>(RootName);
            if (root == null || !(root.userData is GridHudElements))
            {
                root?.RemoveFromHierarchy();
                root = CreateRoot();
                sceneView.rootVisualElement.Add(root);
            }

            GridHudElements elements = (GridHudElements)root.userData;
            root.style.display = CubeMapEditorToolState.IsGridMap
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            elements.Refresh();
        }

        private static VisualElement CreateRoot()
        {
            VisualElement root = new VisualElement { name = RootName };
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.right = 0f;
            root.style.top = 0f;
            root.style.bottom = 0f;
            root.userData = new GridHudElements(root);
            return root;
        }

        private static void RemoveAll()
        {
            foreach (SceneView view in SceneView.sceneViews)
            {
                view.rootVisualElement.Q<VisualElement>(RootName)?.RemoveFromHierarchy();
            }
        }

        private sealed class GridHudElements
        {
            private readonly VisualElement panel;
            private readonly Label statusLabel;
            private readonly Label cellLabel;
            private readonly IntegerField placementSubdivisionField;
            private readonly Label currentItemLabel;
            private readonly Label selectedLabel;
            private readonly VisualElement paletteItems;
            private readonly ObjectField paletteField;
            private readonly ObjectField sourceField;
            private readonly TextField nameField;
            private readonly IntegerField widthField;
            private readonly IntegerField heightField;
            private readonly Toggle createOverlapToggle;
            private readonly Toggle createSnapToggle;
            private readonly EnumField faceField;
            private readonly Toggle overlapToggle;
            private readonly Toggle snapToggle;
            private readonly Label rotationLabel;
            private readonly Image itemPreview;
            private readonly Button clearItemButton;
            private readonly Button deleteSelectedButton;
            private readonly Button useSelectedButton;
            private GridMapPalette displayedPalette;

            internal GridHudElements(VisualElement root)
            {
                Color panelColor = EditorGUIUtility.isProSkin
                    ? new Color(0.1f, 0.12f, 0.15f, 0.97f)
                    : new Color(0.94f, 0.95f, 0.97f, 0.98f);
                panel = CreatePanel(root, panelColor);
                AddTitle(panel, "网格地图搭建", "单元格对齐 · 预览 · 批量摆放");
                statusLabel = CreateBadge("选择一个小拼图 Scene 开始");
                panel.Add(statusLabel);

                VisualElement workspaceCard = CreateCard("当前网格", "自动读取当前工作区的小拼图尺寸");
                cellLabel = new Label();
                cellLabel.style.fontSize = 11f;
                workspaceCard.Add(cellLabel);
                faceField = new EnumField("编辑面", CubeMapFace.Front);
                faceField.RegisterValueChangedCallback(evt =>
                {
                    GridMapEditorState.ActiveFace = (CubeMapFace)evt.newValue;
                    SceneView.RepaintAll();
                });
                workspaceCard.Add(faceField);
                placementSubdivisionField = new IntegerField("放置网格细分")
                {
                    isDelayed = true,
                    value = 4,
                    tooltip = "每个拼图大格再细分的放置步长；数值越大，可摆放的位置越密"
                };
                placementSubdivisionField.RegisterValueChangedCallback(evt =>
                {
                    GridMapPieceContext context = GridMapEditorService.FindActivePiece();
                    if (!context.IsValid)
                    {
                        return;
                    }

                    Undo.RecordObject(context.Workspace, "修改放置网格细分");
                    context.Workspace.SetPlacementGridSubdivisions(evt.newValue);
                    EditorUtility.SetDirty(context.Workspace);
                    AssetDatabase.SaveAssets();
                    SceneView.RepaintAll();
                });
                workspaceCard.Add(placementSubdivisionField);
                panel.Add(workspaceCard);

                VisualElement currentCard = CreateCard("当前物品", "点击物品进入摆放模式；R 或按钮旋转");
                currentCard.style.paddingTop = 10f;
                currentCard.style.paddingBottom = 10f;
                currentCard.style.borderBottomWidth = 3f;
                currentCard.style.borderBottomColor = new Color(0.18f, 0.68f, 1f, 1f);
                VisualElement currentRow = CreateRow();
                itemPreview = new Image { scaleMode = ScaleMode.ScaleToFit };
                itemPreview.style.width = 56f;
                itemPreview.style.height = 56f;
                itemPreview.style.marginRight = 8f;
                currentRow.Add(itemPreview);
                currentItemLabel = new Label("未选择物品");
                currentItemLabel.style.whiteSpace = WhiteSpace.Normal;
                currentItemLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                currentRow.Add(currentItemLabel);
                currentCard.Add(currentRow);
                VisualElement rotationRow = CreateRow();
                rotationLabel = new Label();
                rotationLabel.style.flexGrow = 1f;
                rotationRow.Add(rotationLabel);
                rotationRow.Add(CreateButton("↶", () => GridMapEditorState.Rotate(-1)));
                rotationRow.Add(CreateButton("↷", () => GridMapEditorState.Rotate(1)));
                clearItemButton = CreateButton("停止摆放", () => GridMapEditorState.CurrentItem = null);
                rotationRow.Add(clearItemButton);
                currentCard.Add(rotationRow);
                overlapToggle = new Toggle("允许叠加其他物品")
                {
                    value = GridMapEditorState.AllowOverlap
                };
                overlapToggle.RegisterValueChangedCallback(evt =>
                {
                    GridMapEditorState.AllowOverlap = evt.newValue;
                    SceneView.RepaintAll();
                });
                currentCard.Add(overlapToggle);
                snapToggle = new Toggle("强制吸附到网格")
                {
                    value = GridMapEditorState.SnapToGrid
                };
                snapToggle.RegisterValueChangedCallback(evt =>
                {
                    GridMapEditorState.SnapToGrid = evt.newValue;
                    SceneView.RepaintAll();
                });
                currentCard.Add(snapToggle);
                panel.Add(currentCard);

                VisualElement paletteCard = CreateCard("物品仓库", "由所有已制作的地图物品预制体组成");
                paletteField = new ObjectField("仓库")
                {
                    objectType = typeof(GridMapPalette),
                    allowSceneObjects = false,
                    value = GridMapEditorService.LoadPalette()
                };
                paletteField.RegisterValueChangedCallback(evt =>
                {
                    displayedPalette = evt.newValue as GridMapPalette;
                    RebuildPaletteItems();
                });
                paletteCard.Add(paletteField);
                paletteItems = new VisualElement();
                paletteItems.style.maxHeight = 150f;
                paletteItems.style.overflow = Overflow.Hidden;
                paletteCard.Add(paletteItems);
                paletteCard.Add(CreateButton("＋ 新建物品仓库", () =>
                {
                    paletteField.SetValueWithoutNotify(GridMapEditorService.EnsurePalette());
                    displayedPalette = paletteField.value as GridMapPalette;
                    RebuildPaletteItems();
                }));
                panel.Add(paletteCard);

                Foldout createFoldout = new Foldout
                {
                    text = "快速制作预制体并加入仓库",
                    value = false
                };
                sourceField = new ObjectField("源物体")
                {
                    objectType = typeof(GameObject),
                    allowSceneObjects = true
                };
                createFoldout.Add(sourceField);
                nameField = new TextField("物品名称");
                createFoldout.Add(nameField);
                VisualElement sizeRow = CreateRow();
                widthField = new IntegerField("宽") { value = 1 };
                heightField = new IntegerField("高") { value = 1 };
                sizeRow.Add(widthField);
                sizeRow.Add(heightField);
                createFoldout.Add(sizeRow);
                createOverlapToggle = new Toggle("默认允许叠加") { value = true };
                createSnapToggle = new Toggle("默认吸附网格") { value = true };
                createFoldout.Add(createOverlapToggle);
                createFoldout.Add(createSnapToggle);
                createFoldout.Add(CreateButton("制作预制体并添加", CreateItem));
                panel.Add(createFoldout);

                VisualElement selectedCard = CreateCard("选中物品", "左键查看 · 框选批量选择 · Delete 删除");
                selectedLabel = new Label("暂无选中物品");
                selectedLabel.style.whiteSpace = WhiteSpace.Normal;
                selectedCard.Add(selectedLabel);
                useSelectedButton = CreateButton("将选中物品设为当前", UseSelectedAsCurrent);
                selectedCard.Add(useSelectedButton);
                deleteSelectedButton = CreateButton("删除选中物品", DeleteSelected);
                selectedCard.Add(deleteSelectedButton);
                panel.Add(selectedCard);

                Label help = new Label("左键放置/选择 · 拖动批量放置/框选 · 右键快速删除 · R旋转 · Alt交还Scene View");
                help.style.whiteSpace = WhiteSpace.Normal;
                help.style.fontSize = 10f;
                help.style.opacity = 0.66f;
                help.style.marginTop = 5f;
                panel.Add(help);
                displayedPalette = paletteField.value as GridMapPalette;
                RebuildPaletteItems();
            }

            internal void Refresh()
            {
                GridMapPieceContext context = GridMapEditorService.FindActivePiece();
                bool valid = context.IsValid;
                panel.SetEnabled(valid);
                if (!valid)
                {
                    statusLabel.text = "请打开一个小拼图 Scene";
                    cellLabel.text = "网格编辑只作用于独立小拼图，不修改生成场景";
                    currentItemLabel.text = "未选择物品";
                    deleteSelectedButton.SetEnabled(false);
                    useSelectedButton.SetEnabled(false);
                    return;
                }

                Vector2 cellSize = context.Workspace.CellSize;
                Vector2 placementCellSize = context.Workspace.PlacementCellSize;
                statusLabel.text = $"小拼图 {context.Piece.PieceIndex:00} · {CubeMapLayoutMath.GetFaceLabel(context.Face)}";
                cellLabel.text = $"拼图基准网格 {context.Workspace.ColumnsPerFace} × {context.Workspace.RowsPerPiece} 格\n" +
                                 $"物品尺寸单位 {cellSize.x:0.###} × {cellSize.y:0.###}\n" +
                                 $"放置网格 {context.Workspace.PlacementColumnsPerFace} × " +
                                 $"{context.Workspace.PlacementRowsPerPiece} 格 · 步长 " +
                                 $"{placementCellSize.x:0.###} × {placementCellSize.y:0.###}";
                faceField.SetValueWithoutNotify(GridMapEditorState.ActiveFace);
                placementSubdivisionField.SetValueWithoutNotify(
                    context.Workspace.PlacementGridSubdivisions);
                overlapToggle.SetValueWithoutNotify(GridMapEditorState.AllowOverlap);
                snapToggle.SetValueWithoutNotify(GridMapEditorState.SnapToGrid);
                rotationLabel.text = $"旋转：{GridMapEditorState.RotationSteps * 90}°";
                GridMapItemDefinition current = GridMapEditorState.CurrentItem;
                currentItemLabel.text = current == null
                    ? "未选择物品\n左键点击仓库中的物品"
                    : $"{current.DisplayName}\n占用基准网格 {current.SizeInCells.x} × " +
                      $"{current.SizeInCells.y} 格";
                itemPreview.image = current != null
                    ? AssetPreview.GetAssetPreview(current.Prefab)
                    : null;
                clearItemButton.SetEnabled(current != null);
                GridMapPlacement[] selected = GetSelectedPlacements();
                selectedLabel.text = selected.Length == 0
                    ? "暂无选中物品"
                    : BuildSelectedText(selected);
                deleteSelectedButton.SetEnabled(selected.Length > 0);
                useSelectedButton.SetEnabled(selected.Length > 0 && selected[0].Definition != null);
                if (displayedPalette != paletteField.value)
                {
                    displayedPalette = paletteField.value as GridMapPalette;
                    RebuildPaletteItems();
                }
            }

            private void RebuildPaletteItems()
            {
                paletteItems.Clear();
                if (displayedPalette == null || displayedPalette.Items.Count == 0)
                {
                    paletteItems.Add(new Label("仓库为空，请先制作一个物品预制体。"));
                    return;
                }

                foreach (GridMapItemDefinition definition in displayedPalette.Items)
                {
                    GridMapItemDefinition captured = definition;
                    Button button = CreateButton($"{definition.DisplayName}  ({definition.SizeInCells.x}×{definition.SizeInCells.y})",
                        () =>
                        {
                            GridMapEditorState.CurrentItem = captured;
                            GridMapEditorState.AllowOverlap = captured.AllowOverlapByDefault;
                            GridMapEditorState.SnapToGrid = captured.SnapToGridByDefault;
                            SceneView.RepaintAll();
                        });
                    button.style.unityTextAlign = TextAnchor.MiddleLeft;
                    paletteItems.Add(button);
                }
            }

            private void CreateItem()
            {
                GridMapItemDefinition definition = GridMapEditorService.CreateItemDefinition(
                    sourceField.value as GameObject,
                    nameField.value,
                    new Vector2Int(Mathf.Max(1, widthField.value), Mathf.Max(1, heightField.value)),
                    Vector2.zero,
                    createOverlapToggle.value,
                    createSnapToggle.value);
                if (definition == null)
                {
                    EditorUtility.DisplayDialog("无法制作地图物品", "请先在“源物体”中指定一个场景物体或预制体。", "确定");
                    return;
                }

                GridMapEditorState.CurrentItem = definition;
                displayedPalette = GridMapEditorService.LoadPalette();
                paletteField.SetValueWithoutNotify(displayedPalette);
                RebuildPaletteItems();
                SceneView.RepaintAll();
            }

            private void DeleteSelected()
            {
                GridMapEditorService.DeletePlacements(GetSelectedPlacements());
                SceneView.RepaintAll();
            }

            private void UseSelectedAsCurrent()
            {
                GridMapPlacement[] selected = GetSelectedPlacements();
                if (selected.Length == 0 || selected[0].Definition == null)
                {
                    return;
                }

                GridMapEditorState.CurrentItem = selected[0].Definition;
                GridMapEditorState.RotationSteps = selected[0].RotationSteps;
                GridMapEditorState.SnapToGrid = selected[0].SnappedToGrid;
                GridMapEditorState.AllowOverlap = selected[0].AllowOverlap;
                SceneView.RepaintAll();
            }

            private static string BuildSelectedText(GridMapPlacement[] selected)
            {
                GridMapPlacement first = selected[0];
                if (first == null || first.Definition == null)
                {
                    return $"已选 {selected.Length} 个物品";
                }

                Vector2Int size = first.RotatedSizeInCells;
                return $"已选 {selected.Length} 个\n" +
                       $"{first.Definition.DisplayName} · 占用 {size.x} × {size.y} 格\n" +
                       $"旋转 {first.RotationSteps * 90}° · " +
                       $"{(first.SnappedToGrid ? "已吸附" : "自由摆放")} · " +
                       $"{(first.AllowOverlap ? "允许叠加" : "禁止叠加")}";
            }

            private static GridMapPlacement[] GetSelectedPlacements()
            {
                List<GridMapPlacement> placements = new List<GridMapPlacement>();
                foreach (UnityEngine.Object selected in Selection.objects)
                {
                    GridMapPlacement placement = selected as GridMapPlacement;
                    if (placement == null && selected is GameObject gameObject)
                    {
                        placement = gameObject.GetComponentInParent<GridMapPlacement>();
                    }

                    if (placement != null && !placements.Contains(placement))
                    {
                        placements.Add(placement);
                    }
                }

                return placements.ToArray();
            }

            private static VisualElement CreatePanel(VisualElement root, Color color)
            {
                VisualElement value = new ScrollView(ScrollViewMode.Vertical);
                value.pickingMode = PickingMode.Position;
                value.style.position = Position.Absolute;
                value.style.top = 46f;
                value.style.right = 12f;
                value.style.width = 320f;
                value.style.maxHeight = Length.Percent(92f);
                value.style.backgroundColor = color;
                value.style.paddingLeft = 10f;
                value.style.paddingRight = 10f;
                value.style.paddingTop = 9f;
                value.style.paddingBottom = 9f;
                value.style.borderTopLeftRadius = 8f;
                value.style.borderTopRightRadius = 8f;
                value.style.borderBottomLeftRadius = 8f;
                value.style.borderBottomRightRadius = 8f;
                root.Add(value);
                return value;
            }

            private static VisualElement CreateCard(string title, string subtitle)
            {
                VisualElement card = new VisualElement();
                card.style.marginTop = 6f;
                card.style.paddingLeft = 7f;
                card.style.paddingRight = 7f;
                card.style.paddingTop = 6f;
                card.style.paddingBottom = 6f;
                card.style.backgroundColor = EditorGUIUtility.isProSkin
                    ? new Color(0.17f, 0.19f, 0.22f, 1f)
                    : new Color(0.86f, 0.88f, 0.91f, 1f);
                Label titleLabel = new Label(title);
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                card.Add(titleLabel);
                Label subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 9f;
                subtitleLabel.style.opacity = 0.62f;
                subtitleLabel.style.marginBottom = 4f;
                card.Add(subtitleLabel);
                return card;
            }

            private static void AddTitle(VisualElement panel, string title, string subtitle)
            {
                Label titleLabel = new Label(title);
                titleLabel.style.fontSize = 14f;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                panel.Add(titleLabel);
                Label subtitleLabel = new Label(subtitle);
                subtitleLabel.style.fontSize = 10f;
                subtitleLabel.style.opacity = 0.64f;
                subtitleLabel.style.marginBottom = 4f;
                panel.Add(subtitleLabel);
            }

            private static Label CreateBadge(string text)
            {
                Label badge = new Label(text);
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.style.backgroundColor = new Color(0.18f, 0.68f, 1f, 0.28f);
                badge.style.paddingTop = 4f;
                badge.style.paddingBottom = 4f;
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
