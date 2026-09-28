using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.SurfaceTiles.Editor
{
    internal sealed class SurfaceTileSheetImporterWindow : EditorWindow
    {
        private enum CanvasMode
        {
            Draw = 0,
            Remove = 1
        }

        private const float SidebarWidth = 300f;
        private Texture2D sourceAsset;
        private Texture2D sourcePreview;
        private SurfaceTileSheetImportRecipe recipe;
        private Vector2 canvasScroll;
        private Vector2 sidebarScroll;
        private Vector2 regionScroll;
        private float zoom = 0.35f;
        private CanvasMode canvasMode;
        private int selectedIndex = -1;
        private int hoveredIndex = -1;
        private bool sidebarVisible = true;
        private bool dragging;
        private Vector2 dragStart;
        private Vector2 dragCurrent;
        private int minimumOpaquePixels = 64;
        private int mergeGap = 3;
        private int detectionMargin = 2;
        private string status = "选择源图片后，在图片上拖框即可创建瓦片。";

        [MenuItem("Tools/2026Test/方块贴画/导入不规则瓦片图")]
        internal static void OpenWindow()
        {
            SurfaceTileSheetImporterWindow window =
                GetWindow<SurfaceTileSheetImporterWindow>("不规则瓦片导入");
            window.minSize = new Vector2(1100f, 700f);
            window.Show();
            window.TryUseProjectSelection();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            TryUseProjectSelection();
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is Texture2D texture &&
                texture != sourceAsset)
            {
                SetSource(texture);
                Repaint();
            }
        }

        private void OnGUI()
        {
            HandleKeyboardShortcuts();
            DrawHeader();
            EditorGUILayout.Space(3f);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawCanvasPanel();
                if (sidebarVisible)
                {
                    DrawSidebar();
                }
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "不规则瓦片图导入器",
                    EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    Texture2D next = (Texture2D)EditorGUILayout.ObjectField(
                        "源图片",
                        sourceAsset,
                        typeof(Texture2D),
                        false);
                    if (next != sourceAsset)
                    {
                        SetSource(next);
                    }

                    GUILayout.Label(
                        "原图不改动 · 框选后生成规则瓦片库",
                        EditorStyles.miniLabel,
                        GUILayout.Width(230f));
                }
            }
        }

        private void DrawCanvasPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    canvasMode = (CanvasMode)GUILayout.Toolbar(
                        (int)canvasMode,
                        new[] { "＋ 框选添加", "－ 点框取消" },
                        EditorStyles.toolbarButton,
                        GUILayout.Width(210f));
                    GUILayout.Space(8f);
                    if (GUILayout.Button("－", EditorStyles.toolbarButton, GUILayout.Width(25f)))
                    {
                        SetZoom(zoom / 1.25f);
                    }

                    GUILayout.Label($"{zoom:P0}", GUILayout.Width(42f));
                    if (GUILayout.Button("＋", EditorStyles.toolbarButton, GUILayout.Width(25f)))
                    {
                        SetZoom(zoom * 1.25f);
                    }

                    if (GUILayout.Button("25%", EditorStyles.toolbarButton, GUILayout.Width(38f)))
                    {
                        SetZoom(0.25f);
                    }

                    if (GUILayout.Button("50%", EditorStyles.toolbarButton, GUILayout.Width(38f)))
                    {
                        SetZoom(0.5f);
                    }

                    if (GUILayout.Button("100%", EditorStyles.toolbarButton, GUILayout.Width(44f)))
                    {
                        SetZoom(1f);
                    }

                    if (GUILayout.Button("适合宽度", EditorStyles.toolbarButton))
                    {
                        FitZoom();
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(
                        canvasMode == CanvasMode.Draw
                            ? "空白处拖框；点已有框可选中"
                            : "点击绿框立即取消",
                        EditorStyles.miniLabel);
                    if (GUILayout.Button(
                            sidebarVisible ? "隐藏设置" : "显示设置",
                            EditorStyles.toolbarButton,
                            GUILayout.Width(66f)))
                    {
                        sidebarVisible = !sidebarVisible;
                    }
                }

                canvasScroll = EditorGUILayout.BeginScrollView(
                    canvasScroll,
                    true,
                    true,
                    GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(true));
                if (sourcePreview == null)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.HelpBox(
                        "在上方选择 Assets 内的 PNG/JPG。也可以先在 Project 窗口选中图片，再打开本工具。",
                        MessageType.Info);
                    GUILayout.FlexibleSpace();
                }
                else
                {
                    float displayWidth = sourcePreview.width * zoom;
                    float displayHeight = sourcePreview.height * zoom;
                    Rect imageRect = GUILayoutUtility.GetRect(
                        displayWidth,
                        displayHeight,
                        GUILayout.Width(displayWidth),
                        GUILayout.Height(displayHeight));
                    EditorGUI.DrawRect(imageRect, new Color(0.08f, 0.09f, 0.1f, 1f));
                    GUI.DrawTexture(
                        imageRect,
                        sourcePreview,
                        ScaleMode.StretchToFill,
                        true);
                    HandleCanvasInput(imageRect);
                    DrawRegionOverlays(imageRect);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSidebar()
        {
            using (new EditorGUILayout.VerticalScope(
                       EditorStyles.helpBox,
                       GUILayout.Width(SidebarWidth),
                       GUILayout.ExpandHeight(true)))
            {
                if (recipe == null)
                {
                    EditorGUILayout.HelpBox(
                        "选择源图片后会在原图旁创建一个可提交的导入方案，用来保存选区和输出设置。",
                        MessageType.Info);
                    return;
                }

                sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll);
                DrawSelectedRegionActions();
                DrawOutputSettings();
                EditorGUILayout.Space(6f);
                DrawDetectionSettings();
                EditorGUILayout.Space(6f);
                DrawRegionList();
                EditorGUILayout.EndScrollView();
                EditorGUILayout.HelpBox(status, MessageType.None);
                GUI.enabled = CountEnabledRegions() > 0;
                Color previous = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.2f, 0.75f, 0.95f, 1f);
                if (GUILayout.Button("生成规则图集和瓦片库", GUILayout.Height(38f)))
                {
                    Generate();
                }

                GUI.backgroundColor = previous;
                GUI.enabled = true;
            }
        }

        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);
            string folder = EditorGUILayout.TextField(
                "输出目录",
                recipe.OutputFolder);
            string name = EditorGUILayout.TextField(
                "资源名称",
                recipe.OutputName);
            int width = Mathf.Max(8, EditorGUILayout.IntField(
                "统一瓦片宽",
                recipe.OutputWidth));
            int height = Mathf.Max(8, EditorGUILayout.IntField(
                "统一瓦片高",
                recipe.OutputHeight));
            int padding = Mathf.Max(0, EditorGUILayout.IntField(
                "透明边距",
                recipe.TransparentPadding));
            bool trim = EditorGUILayout.Toggle(
                "自动裁掉选区空白",
                recipe.TrimTransparentPixels);
            bool upscale = EditorGUILayout.Toggle(
                "允许放大小素材",
                recipe.AllowUpscale);
            SurfaceTileOutputAnchor anchor =
                (SurfaceTileOutputAnchor)EditorGUILayout.EnumPopup(
                    "对齐方式",
                    recipe.Anchor);
            if (folder != recipe.OutputFolder || name != recipe.OutputName ||
                width != recipe.OutputWidth || height != recipe.OutputHeight ||
                padding != recipe.TransparentPadding ||
                trim != recipe.TrimTransparentPixels ||
                upscale != recipe.AllowUpscale || anchor != recipe.Anchor)
            {
                Undo.RecordObject(recipe, "修改瓦片导入设置");
                recipe.ConfigureOutput(
                    folder,
                    name,
                    width,
                    height,
                    padding,
                    trim,
                    upscale,
                    anchor);
                SaveRecipe();
            }
        }

        private void DrawDetectionSettings()
        {
            EditorGUILayout.LabelField("辅助识别（可选）", EditorStyles.boldLabel);
            minimumOpaquePixels = Mathf.Max(1, EditorGUILayout.IntField(
                "忽略小于像素数",
                minimumOpaquePixels));
            mergeGap = Mathf.Max(0, EditorGUILayout.IntField(
                "合并相邻间隔",
                mergeGap));
            detectionMargin = Mathf.Max(0, EditorGUILayout.IntField(
                "选区外扩像素",
                detectionMargin));
            EditorGUILayout.LabelField(
                "这类概念图含文字，自动结果需要人工删减；通常直接拖框更快。",
                EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = sourcePreview != null;
                if (GUILayout.Button("自动识别透明块"))
                {
                    AutoDetect();
                }

                GUI.enabled = recipe.Regions.Count > 0;
                if (GUILayout.Button("清空选区"))
                {
                    if (EditorUtility.DisplayDialog(
                            "清空全部选区？",
                            "只清除导入方案中的选区，不会删除原图或已生成资源。",
                            "清空",
                            "取消"))
                    {
                        Undo.RecordObject(recipe, "清空瓦片选区");
                        recipe.Regions.Clear();
                        selectedIndex = -1;
                        SaveRecipe();
                    }
                }

                GUI.enabled = true;
            }
        }

        private void DrawRegionList()
        {
            EditorGUILayout.LabelField(
                $"瓦片选区 · 启用 {CountEnabledRegions()} / {recipe.Regions.Count}",
                EditorStyles.boldLabel);
            regionScroll = EditorGUILayout.BeginScrollView(
                regionScroll,
                GUILayout.MinHeight(130f),
                GUILayout.MaxHeight(240f));
            for (int index = 0; index < recipe.Regions.Count; index++)
            {
                SurfaceTileSourceRegion region = recipe.Regions[index];
                Color previous = GUI.backgroundColor;
                if (index == selectedIndex)
                {
                    GUI.backgroundColor = new Color(0.25f, 0.75f, 1f, 1f);
                }

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUI.backgroundColor = previous;
                    bool enabled = EditorGUILayout.Toggle(
                        region.Enabled,
                        GUILayout.Width(18f));
                    string regionName = EditorGUILayout.TextField(region.DisplayName);
                    if (GUILayout.Button("定位", GUILayout.Width(40f)))
                    {
                        selectedIndex = index;
                        FocusRegion(region.Rect);
                    }

                    if (GUILayout.Button("×", GUILayout.Width(24f)))
                    {
                        RemoveRegion(index);
                        GUIUtility.ExitGUI();
                    }

                    if (enabled != region.Enabled ||
                        regionName != region.DisplayName)
                    {
                        Undo.RecordObject(recipe, "修改瓦片选区");
                        region.Enabled = enabled;
                        region.DisplayName = regionName;
                        SaveRecipe();
                    }
                }

                GUI.backgroundColor = previous;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRegionOverlays(Rect imageRect)
        {
            if (recipe == null)
            {
                return;
            }

            for (int index = 0; index < recipe.Regions.Count; index++)
            {
                SurfaceTileSourceRegion region = recipe.Regions[index];
                Rect display = SourceToDisplay(region.Rect, imageRect);
                bool destructiveHover = index == hoveredIndex &&
                                        canvasMode == CanvasMode.Remove;
                Color color = index == selectedIndex
                    ? new Color(0.1f, 0.85f, 1f, 0.22f)
                    : destructiveHover
                        ? new Color(1f, 0.18f, 0.12f, 0.32f)
                    : region.Enabled
                        ? new Color(0.2f, 1f, 0.45f, 0.12f)
                        : new Color(0.5f, 0.5f, 0.5f, 0.12f);
                EditorGUI.DrawRect(display, color);
                Color outline = destructiveHover
                    ? new Color(1f, 0.2f, 0.12f, 1f)
                    : index == selectedIndex
                    ? new Color(0.1f, 0.9f, 1f, 1f)
                    : region.Enabled
                        ? new Color(0.25f, 1f, 0.5f, 0.9f)
                        : new Color(0.6f, 0.6f, 0.6f, 0.7f);
                DrawClippedOutline(display, outline, 2f);
                if (zoom >= 0.2f && display.width >= 32f)
                {
                    GUI.Label(
                        new Rect(display.x + 3f, display.y + 2f, display.width - 6f, 18f),
                        $"{index + 1} {region.DisplayName}",
                        EditorStyles.whiteMiniLabel);
                }

                if (destructiveHover && display.width >= 18f && display.height >= 18f)
                {
                    GUI.Label(
                        new Rect(display.xMax - 20f, display.yMin + 1f, 18f, 18f),
                        "×",
                        EditorStyles.whiteLargeLabel);
                }
            }

            if (dragging)
            {
                Rect drag = Rect.MinMaxRect(
                    Mathf.Min(dragStart.x, dragCurrent.x),
                    Mathf.Min(dragStart.y, dragCurrent.y),
                    Mathf.Max(dragStart.x, dragCurrent.x),
                    Mathf.Max(dragStart.y, dragCurrent.y));
                EditorGUI.DrawRect(drag, new Color(0.1f, 0.7f, 1f, 0.2f));
                DrawClippedOutline(drag, Color.cyan, 2f);
            }
        }

        private void HandleCanvasInput(Rect imageRect)
        {
            Event evt = Event.current;
            hoveredIndex = imageRect.Contains(evt.mousePosition)
                ? FindRegionAt(evt.mousePosition, imageRect)
                : -1;
            if (evt.type == EventType.MouseMove)
            {
                Repaint();
            }
            if (!imageRect.Contains(evt.mousePosition) && !dragging)
            {
                return;
            }

            if (evt.type == EventType.MouseDown && evt.button == 1)
            {
                int index = FindRegionAt(evt.mousePosition, imageRect);
                if (index >= 0)
                {
                    RemoveRegion(index);
                    evt.Use();
                }

                return;
            }

            if (canvasMode == CanvasMode.Remove &&
                evt.type == EventType.MouseDown && evt.button == 0)
            {
                int index = FindRegionAt(evt.mousePosition, imageRect);
                if (index >= 0)
                {
                    RemoveRegion(index);
                    evt.Use();
                }

                return;
            }

            if (canvasMode != CanvasMode.Draw || evt.button != 0)
            {
                return;
            }

            if (evt.type == EventType.MouseDown)
            {
                int existing = FindRegionAt(evt.mousePosition, imageRect);
                if (existing >= 0 && !evt.shift)
                {
                    selectedIndex = existing;
                    Repaint();
                    evt.Use();
                    return;
                }

                dragging = true;
                dragStart = evt.mousePosition;
                dragCurrent = dragStart;
                evt.Use();
            }
            else if (evt.type == EventType.MouseDrag && dragging)
            {
                dragCurrent = new Vector2(
                    Mathf.Clamp(evt.mousePosition.x, imageRect.xMin, imageRect.xMax),
                    Mathf.Clamp(evt.mousePosition.y, imageRect.yMin, imageRect.yMax));
                evt.Use();
                Repaint();
            }
            else if (evt.type == EventType.MouseUp && dragging)
            {
                dragCurrent = new Vector2(
                    Mathf.Clamp(evt.mousePosition.x, imageRect.xMin, imageRect.xMax),
                    Mathf.Clamp(evt.mousePosition.y, imageRect.yMin, imageRect.yMax));
                dragging = false;
                Rect display = Rect.MinMaxRect(
                    Mathf.Min(dragStart.x, dragCurrent.x),
                    Mathf.Min(dragStart.y, dragCurrent.y),
                    Mathf.Max(dragStart.x, dragCurrent.x),
                    Mathf.Max(dragStart.y, dragCurrent.y));
                if (display.width >= 3f && display.height >= 3f)
                {
                    AddRegion(DisplayToSource(display, imageRect));
                }

                evt.Use();
                Repaint();
            }
        }

        private void SetSource(Texture2D texture)
        {
            DestroyPreview();
            sourceAsset = texture;
            recipe = null;
            selectedIndex = -1;
            if (texture == null)
            {
                status = "请选择源图片。";
                return;
            }

            if (!SurfaceTileSheetGenerator.TryLoadOriginal(
                    texture,
                    out sourcePreview,
                    out string error))
            {
                status = error;
                return;
            }

            string sourcePath = AssetDatabase.GetAssetPath(texture);
            string recipePath = Path.GetDirectoryName(sourcePath)
                ?.Replace('\\', '/') + "/" +
                Path.GetFileNameWithoutExtension(sourcePath) +
                "_SurfaceTileImport.asset";
            recipe = AssetDatabase.LoadAssetAtPath<SurfaceTileSheetImportRecipe>(
                recipePath);
            if (recipe == null)
            {
                recipe = CreateInstance<SurfaceTileSheetImportRecipe>();
                recipe.ConfigureSource(texture);
                AssetDatabase.CreateAsset(recipe, recipePath);
                AssetDatabase.SaveAssets();
            }
            else if (recipe.SourceTexture != texture)
            {
                Undo.RecordObject(recipe, "更新瓦片源图片");
                recipe.ConfigureSource(texture);
                SaveRecipe();
            }

            status = $"原图 {sourcePreview.width}×{sourcePreview.height} · " +
                     "拖框可把连续平台或整栋建筑作为一个瓦片。";
            SetZoom(Mathf.Max(0.35f, CalculateFitZoom()));
        }

        private void TryUseProjectSelection()
        {
            if (sourceAsset == null && Selection.activeObject is Texture2D texture)
            {
                SetSource(texture);
            }
        }

        private void AddRegion(RectInt rect)
        {
            rect = ClampToSource(rect);
            if (rect.width <= 0 || rect.height <= 0)
            {
                return;
            }

            Undo.RecordObject(recipe, "添加瓦片选区");
            string name = "Tile_" + (recipe.Regions.Count + 1).ToString("D3");
            recipe.Regions.Add(new SurfaceTileSourceRegion(name, rect));
            selectedIndex = recipe.Regions.Count - 1;
            SaveRecipe();
        }

        private void AutoDetect()
        {
            if (sourcePreview == null || recipe == null)
            {
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar(
                    "识别不规则瓦片",
                    "正在读取透明区域……",
                    0.45f);
                List<RectInt> detected = SurfaceTileSheetGenerator.DetectRegions(
                    sourcePreview,
                    0,
                    minimumOpaquePixels,
                    mergeGap,
                    detectionMargin);
                if (detected.Count == 0)
                {
                    status = "没有识别到满足条件的透明块。";
                    return;
                }

                bool replace = recipe.Regions.Count == 0 ||
                               EditorUtility.DisplayDialog(
                                   "自动识别完成",
                                   $"识别到 {detected.Count} 个候选区域。\n" +
                                   "替换现有选区，还是追加？",
                                   "替换",
                                   "追加");
                Undo.RecordObject(recipe, "自动识别瓦片选区");
                if (replace)
                {
                    recipe.Regions.Clear();
                }

                int start = recipe.Regions.Count;
                for (int index = 0; index < detected.Count; index++)
                {
                    recipe.Regions.Add(new SurfaceTileSourceRegion(
                        "Tile_" + (start + index + 1).ToString("D3"),
                        detected[index]));
                }

                selectedIndex = recipe.Regions.Count > 0 ? 0 : -1;
                SaveRecipe();
                status = $"已识别 {detected.Count} 个候选区域。概念图中的文字可能被识别，请在右侧删除或禁用。";
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void Generate()
        {
            SaveRecipe();
            if (!SurfaceTileSheetGenerator.Generate(
                    recipe,
                    out SurfaceTileSheetGenerateResult result,
                    out string error))
            {
                status = error;
                EditorUtility.DisplayDialog("生成失败", error, "确定");
                return;
            }

            status = $"已生成 {result.TileCount} 个瓦片：{result.PalettePath}";
            Selection.activeObject = result.Palette;
            EditorGUIUtility.PingObject(result.Palette);
            EditorUtility.DisplayDialog(
                "瓦片库已生成",
                $"共 {result.TileCount} 个瓦片。\n\n" +
                $"图集：{result.TexturePath}\n" +
                $"瓦片库：{result.PalettePath}\n\n" +
                "现在可回到 Scene 贴画面板，把该瓦片库指定给方块。",
                "确定");
        }

        private Rect SourceToDisplay(RectInt source, Rect imageRect)
        {
            float x = imageRect.x + source.x / (float)sourcePreview.width * imageRect.width;
            float y = imageRect.y +
                      (sourcePreview.height - source.yMax) /
                      (float)sourcePreview.height * imageRect.height;
            return new Rect(
                x,
                y,
                source.width / (float)sourcePreview.width * imageRect.width,
                source.height / (float)sourcePreview.height * imageRect.height);
        }

        private RectInt DisplayToSource(Rect display, Rect imageRect)
        {
            int xMin = Mathf.FloorToInt(
                (display.xMin - imageRect.x) / imageRect.width * sourcePreview.width);
            int xMax = Mathf.CeilToInt(
                (display.xMax - imageRect.x) / imageRect.width * sourcePreview.width);
            int yMin = Mathf.FloorToInt(
                (imageRect.yMax - display.yMax) / imageRect.height * sourcePreview.height);
            int yMax = Mathf.CeilToInt(
                (imageRect.yMax - display.yMin) / imageRect.height * sourcePreview.height);
            return ClampToSource(new RectInt(
                xMin,
                yMin,
                xMax - xMin,
                yMax - yMin));
        }

        private RectInt ClampToSource(RectInt rect)
        {
            if (sourcePreview == null)
            {
                return rect;
            }

            int xMin = Mathf.Clamp(rect.xMin, 0, sourcePreview.width);
            int yMin = Mathf.Clamp(rect.yMin, 0, sourcePreview.height);
            int xMax = Mathf.Clamp(rect.xMax, 0, sourcePreview.width);
            int yMax = Mathf.Clamp(rect.yMax, 0, sourcePreview.height);
            return new RectInt(
                xMin,
                yMin,
                Mathf.Max(0, xMax - xMin),
                Mathf.Max(0, yMax - yMin));
        }

        private int FindRegionAt(Vector2 point, Rect imageRect)
        {
            for (int index = recipe.Regions.Count - 1; index >= 0; index--)
            {
                if (SourceToDisplay(recipe.Regions[index].Rect, imageRect)
                    .Contains(point))
                {
                    return index;
                }
            }

            return -1;
        }

        private void FocusRegion(RectInt rect)
        {
            float displayX = rect.x * zoom;
            float displayY = (sourcePreview.height - rect.yMax) * zoom;
            canvasScroll = new Vector2(
                Mathf.Max(0f, displayX - 100f),
                Mathf.Max(0f, displayY - 100f));
            Repaint();
        }

        private int CountEnabledRegions()
        {
            if (recipe == null)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < recipe.Regions.Count; index++)
            {
                if (recipe.Regions[index] != null &&
                    recipe.Regions[index].Enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private void FitZoom()
        {
            if (sourcePreview == null)
            {
                return;
            }

            SetZoom(CalculateFitZoom());
            canvasScroll = Vector2.zero;
        }

        private float CalculateFitZoom()
        {
            if (sourcePreview == null)
            {
                return 0.35f;
            }

            float reserved = sidebarVisible ? SidebarWidth + 55f : 35f;
            float available = Mathf.Max(500f, position.width - reserved);
            return Mathf.Clamp(available / sourcePreview.width, 0.1f, 1.5f);
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, 0.1f, 2f);
            Repaint();
        }

        private void DrawSelectedRegionActions()
        {
            if (selectedIndex < 0 || selectedIndex >= recipe.Regions.Count)
            {
                EditorGUILayout.HelpBox(
                    "操作：框选添加 · 点绿框选中 · 右键直接删除 · Delete 删除选中。",
                    MessageType.Info);
                return;
            }

            SurfaceTileSourceRegion region = recipe.Regions[selectedIndex];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    $"当前选中：{region.DisplayName}",
                    EditorStyles.boldLabel);
                RectInt nextRect = EditorGUILayout.RectIntField("像素范围", region.Rect);
                if (nextRect != region.Rect)
                {
                    Undo.RecordObject(recipe, "修改瓦片选区");
                    region.Rect = ClampToSource(nextRect);
                    SaveRecipe();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(region.Enabled ? "临时禁用" : "重新启用"))
                    {
                        Undo.RecordObject(recipe, "切换瓦片选区");
                        region.Enabled = !region.Enabled;
                        SaveRecipe();
                    }

                    Color previous = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.9f, 0.28f, 0.22f, 1f);
                    if (GUILayout.Button("删除选中"))
                    {
                        RemoveRegion(selectedIndex);
                        GUIUtility.ExitGUI();
                    }

                    GUI.backgroundColor = previous;
                }
            }
        }

        private void HandleKeyboardShortcuts()
        {
            Event evt = Event.current;
            if (evt.type != EventType.KeyDown ||
                EditorGUIUtility.editingTextField ||
                (evt.keyCode != KeyCode.Delete && evt.keyCode != KeyCode.Backspace))
            {
                return;
            }

            if (selectedIndex >= 0 && recipe != null &&
                selectedIndex < recipe.Regions.Count)
            {
                RemoveRegion(selectedIndex);
                evt.Use();
            }
        }

        private void RemoveRegion(int index)
        {
            if (recipe == null || index < 0 || index >= recipe.Regions.Count)
            {
                return;
            }

            Undo.RecordObject(recipe, "删除瓦片选区");
            recipe.Regions.RemoveAt(index);
            if (selectedIndex == index)
            {
                selectedIndex = -1;
            }
            else if (selectedIndex > index)
            {
                selectedIndex--;
            }

            hoveredIndex = -1;
            status = "已取消一个瓦片选区；可按 Ctrl+Z 撤销。";
            SaveRecipe();
        }

        private static void DrawClippedOutline(Rect rect, Color color, float width)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - width, rect.width, width), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, width, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - width, rect.yMin, width, rect.height), color);
        }

        private void SaveRecipe()
        {
            if (recipe == null)
            {
                return;
            }

            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssetIfDirty(recipe);
            Repaint();
        }

        private void DestroyPreview()
        {
            if (sourcePreview != null)
            {
                DestroyImmediate(sourcePreview);
                sourcePreview = null;
            }
        }
    }
}
