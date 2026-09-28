using Project.CameraModes;
using Project.GameFlow;
using Project.GameFlow.Editor;
using Project.PlatformPaths;
using Project.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class PlayableGameScaffolder
{
    public const string PlayerPrefabPath =
        "Assets/_Project/Content/Player/RuntimePlayer.prefab";
    private const string MaterialFolder =
        "Assets/_Project/Content/Materials/Prototype";
    private const string PlayerMaterialPath = MaterialFolder + "/Player.mat";
    private const string PlayerAccentMaterialPath = MaterialFolder + "/PlayerAccent.mat";
    private const string GroundMaterialPath = MaterialFolder + "/Ground.mat";
    private const string PlatformMaterialPath = MaterialFolder + "/Platform.mat";
    private const string GoalMaterialPath = MaterialFolder + "/Goal.mat";
    private const string BackdropMaterialPath = MaterialFolder + "/Backdrop.mat";

    [MenuItem("Tools/2026Test/游戏流程/重建可运行原型（三关）", priority = 40)]
    public static void BuildPlayablePrototype()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "重建可运行原型",
                "将重建 Systems_UI，并重置 Level_01、Level_02、Level_03 的 LevelContent。" +
                "正式关卡开始制作后不要再执行。是否继续？",
                "重建",
                "取消"))
        {
            return;
        }

        BuildInternal();
    }

    public static void BuildPlayablePrototypeBatch()
    {
        BuildInternal();
    }

    private static void BuildInternal()
    {
        GameFlowSceneScaffolder.Generate();
        EnsureFolderForAsset(PlayerPrefabPath);
        EnsureFolder(MaterialFolder);

        Material playerMaterial = GetOrCreateMaterial(
            PlayerMaterialPath,
            new Color(0.12f, 0.68f, 0.95f));
        Material playerAccent = GetOrCreateMaterial(
            PlayerAccentMaterialPath,
            new Color(1f, 0.88f, 0.32f));
        Material ground = GetOrCreateMaterial(
            GroundMaterialPath,
            new Color(0.13f, 0.22f, 0.31f));
        Material platform = GetOrCreateMaterial(
            PlatformMaterialPath,
            new Color(0.13f, 0.55f, 0.68f));
        Material goal = GetOrCreateMaterial(
            GoalMaterialPath,
            new Color(0.25f, 1f, 0.5f),
            true);
        Material backdrop = GetOrCreateMaterial(
            BackdropMaterialPath,
            new Color(0.055f, 0.085f, 0.13f));

        GameObject playerPrefab = CreatePlayerPrefab(playerMaterial, playerAccent);
        PlayableGameUiBuilder.RebuildScene();
        ConfigureCameraScene();
        BuildLevel(
            GameFlowSceneId.Level01,
            GameFlowSceneScaffolder.Level01Path,
            GameFlowSceneId.Level02,
            false,
            playerPrefab,
            ground,
            platform,
            goal,
            backdrop);
        BuildLevel(
            GameFlowSceneId.Level02,
            GameFlowSceneScaffolder.Level02Path,
            GameFlowSceneId.Level03,
            false,
            playerPrefab,
            ground,
            platform,
            goal,
            backdrop);
        BuildLevel(
            GameFlowSceneId.Level03,
            GameFlowSceneScaffolder.Level03Path,
            GameFlowSceneId.Ending,
            true,
            playerPrefab,
            ground,
            platform,
            goal,
            backdrop);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (!GameFlowProjectValidator.Validate(out string report))
        {
            throw new System.InvalidOperationException(report);
        }

        Debug.Log(
            "可运行原型已生成：主菜单 -> Level 1 -> Level 2 -> Level 3 -> Ending。\n" +
            report);
    }

    private static GameObject CreatePlayerPrefab(
        Material bodyMaterial,
        Material accentMaterial)
    {
        GameObject player = new GameObject("RuntimePlayer");
        player.tag = "Player";

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;

        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.radius = 0.38f;
        capsule.height = 1.8f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        BoxCollider interaction = player.AddComponent<BoxCollider>();
        interaction.isTrigger = true;
        interaction.center = new Vector3(0f, 0.9f, 0f);
        interaction.size = new Vector3(0.9f, 1.9f, 0.8f);

        player.AddComponent<PlayableDirector>();
        player.AddComponent<PlayerActionRunner>();
        TimelineActorHost actorHost = player.AddComponent<TimelineActorHost>();
        actorHost.attackPoint = player.transform;
        player.AddComponent<PlatformRider>();
        player.AddComponent<PlayerController>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        visual.transform.localScale = new Vector3(0.72f, 0.85f, 0.42f);
        visual.GetComponent<MeshRenderer>().sharedMaterial = bodyMaterial;

        GameObject directionMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        directionMarker.name = "DirectionMarker";
        Object.DestroyImmediate(directionMarker.GetComponent<Collider>());
        directionMarker.transform.SetParent(player.transform, false);
        directionMarker.transform.localPosition = new Vector3(0.22f, 1.05f, -0.38f);
        directionMarker.transform.localScale = new Vector3(0.16f, 0.16f, 0.08f);
        directionMarker.GetComponent<MeshRenderer>().sharedMaterial = accentMaterial;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
        Object.DestroyImmediate(player);
        if (prefab == null)
        {
            throw new System.InvalidOperationException("无法创建标准玩家 Prefab。");
        }
        return prefab;
    }

    private static void ConfigureCameraScene()
    {
        Scene scene = EditorSceneManager.OpenScene(
            GameFlowSceneScaffolder.CameraPath,
            OpenSceneMode.Single);
        Camera camera = Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            throw new System.InvalidOperationException("Systems_Camera 中没有 Camera。");
        }

        CameraControlManager manager =
            camera.GetComponent<CameraControlManager>() ??
            camera.gameObject.AddComponent<CameraControlManager>();
        CameraModeController mode =
            camera.GetComponent<CameraModeController>() ??
            camera.gameObject.AddComponent<CameraModeController>();
        CameraFollowController follow =
            camera.GetComponent<CameraFollowController>() ??
            camera.gameObject.AddComponent<CameraFollowController>();
        manager.ConfigureOutput(camera);
        follow.Configure(manager, mode, null);
        follow.SetRequestedMode(CameraViewMode.Side2D, 0f, true);
        camera.backgroundColor = new Color(0.08f, 0.12f, 0.17f, 1f);
        EditorSceneManager.SaveScene(scene);
    }

    private static void BuildLevel(
        GameFlowSceneId id,
        string path,
        GameFlowSceneId next,
        bool endsGame,
        GameObject playerPrefab,
        Material groundMaterial,
        Material platformMaterial,
        Material goalMaterial,
        Material backdropMaterial)
    {
        Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        LevelSceneContext context = Object.FindFirstObjectByType<LevelSceneContext>();
        if (context == null)
        {
            GameObject root = new GameObject($"__Level_{id}");
            root.AddComponent<GameFlowSceneRoot>().Configure(id);
            context = root.AddComponent<LevelSceneContext>();
        }

        Transform content = context.transform.Find("LevelContent");
        if (content == null)
        {
            GameObject contentObject = new GameObject("LevelContent");
            contentObject.transform.SetParent(context.transform, false);
            content = contentObject.transform;
        }
        ClearChildren(content);

        Transform oldSpawn = context.transform.Find("PlayerSpawn");
        if (oldSpawn != null)
        {
            Object.DestroyImmediate(oldSpawn.gameObject);
        }

        GameObject spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(content, false);
        spawn.transform.position = new Vector3(-7.3f, 0.25f, 0f);
        spawn.AddComponent<PlayerSpawnPoint>();
        context.Configure(id, spawn.transform, playerPrefab);

        CreateCube(
            content,
            "Backdrop",
            new Vector3(0f, 4.5f, 2.8f),
            new Vector3(21f, 10f, 0.5f),
            backdropMaterial,
            false);
        CreateCube(
            content,
            "Ground",
            new Vector3(0f, -0.35f, 0f),
            new Vector3(19f, 0.7f, 3f),
            groundMaterial,
            true);

        if (id == GameFlowSceneId.Level01)
        {
            CreateCube(content, "Step_A", new Vector3(-1.8f, 0.25f, 0f), new Vector3(2f, 0.5f, 2.8f), platformMaterial, true);
            CreateCube(content, "Step_B", new Vector3(1.2f, 0.65f, 0f), new Vector3(2f, 1.3f, 2.8f), platformMaterial, true);
            CreateCube(content, "Step_C", new Vector3(4.1f, 0.25f, 0f), new Vector3(1.6f, 0.5f, 2.8f), platformMaterial, true);
        }
        else if (id == GameFlowSceneId.Level02)
        {
            CreateCube(content, "Block_A", new Vector3(-2.8f, 0.5f, 0f), new Vector3(1.2f, 1f, 2.8f), platformMaterial, true);
            CreateCube(content, "Block_B", new Vector3(0.2f, 0.85f, 0f), new Vector3(1.2f, 1.7f, 2.8f), platformMaterial, true);
            CreateCube(content, "Block_C", new Vector3(3.3f, 0.5f, 0f), new Vector3(1.2f, 1f, 2.8f), platformMaterial, true);
            CreateCube(content, "UpperRoute", new Vector3(1.5f, 2.2f, 0f), new Vector3(5.5f, 0.35f, 2.8f), platformMaterial, true);
        }
        else
        {
            CreateCube(content, "Stair_A", new Vector3(-3.3f, 0.25f, 0f), new Vector3(1.8f, 0.5f, 2.8f), platformMaterial, true);
            CreateCube(content, "Stair_B", new Vector3(-0.9f, 0.65f, 0f), new Vector3(1.8f, 1.3f, 2.8f), platformMaterial, true);
            CreateCube(content, "Stair_C", new Vector3(1.5f, 1.05f, 0f), new Vector3(1.8f, 2.1f, 2.8f), platformMaterial, true);
            CreateCube(content, "FinalRun", new Vector3(4.4f, 1.55f, 0f), new Vector3(4.5f, 0.4f, 2.8f), platformMaterial, true);
        }

        GameObject goalObject = CreateCube(
            content,
            endsGame ? "LevelGoal_EndGame" : $"LevelGoal_To_{next}",
            new Vector3(8f, endsGame ? 2.9f : 1.25f, 0f),
            new Vector3(1.15f, 2.5f, 1.15f),
            goalMaterial,
            true);
        BoxCollider goalCollider = goalObject.GetComponent<BoxCollider>();
        goalCollider.isTrigger = true;
        LevelGoal levelGoal = goalObject.AddComponent<LevelGoal>();
        levelGoal.Configure(next, endsGame, 0.2f, goalObject.transform);

        GameObject lightObject = new GameObject("Directional Light");
        lightObject.transform.SetParent(content, false);
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;

        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject CreateCube(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool keepCollider)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());
        }
        return cube;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int index = parent.childCount - 1; index >= 0; index--)
        {
            Object.DestroyImmediate(parent.GetChild(index).gameObject);
        }
    }

    private static Material GetOrCreateMaterial(
        string path,
        Color color,
        bool emission = false)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void EnsureFolderForAsset(string assetPath)
    {
        string directory = System.IO.Path.GetDirectoryName(assetPath)
            ?.Replace('\\', '/');
        EnsureFolder(directory);
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        string[] segments = folder.Split('/');
        string current = segments[0];
        for (int index = 1; index < segments.Length; index++)
        {
            string next = current + "/" + segments[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, segments[index]);
            }
            current = next;
        }
    }
}
