using System.Collections.Generic;
using Project.CameraModes;
using Project.PlatformPaths;
using Project.Player;
using Project.RopePaths;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public static class RopeDemoSceneBuilder
{
    private sealed class TrackData
    {
        public int Index;
        public string Name;
        public RopePathNetwork Network;
        public List<RopeSegment> Segments;
        public List<PlatformMove> Platforms;
        public Vector3 Forward;
    }

    private const string ScenePath =
        "Assets/GJ_Tools/TimelineTools3D/RoyRubbish/TestSceneRoy.unity";
    private const string RopeMaterialPath =
        "Assets/_Project/Content/Materials/RopePaths/Rope_White.mat";
    private const string MaterialFolder =
        "Assets/GJ_Tools/TimelineTools3D/RoyRubbish/Materials";

    [MenuItem("Tools/2026Test/RoyRubbish/绳子试玩")]
    public static void BuildDemoScene()
    {
        if (!EditorUtility.DisplayDialog(
                "绳子试玩",
                "将在当前世界生成绳子试玩场景，是否生成？",
                "生成",
                "取消"))
        {
            return;
        }

        Scene scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single);
        CreateLight();

        Material ropeMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(
                RopeMaterialPath);
        Material playerMaterial = GetOrCreateMaterial(
            "RopeDemo_Player.mat",
            new Color(0.02f, 0.65f, 1f, 1f));
        Material platformMaterial = GetOrCreateMaterial(
            "RopeDemo_Platform.mat",
            new Color(1f, 0.42f, 0.02f, 1f));
        Material plateMaterial = GetOrCreateMaterial(
            "RopeDemo_PressurePlate.mat",
            new Color(1f, 0.02f, 0.32f, 1f));

        GameObject root = new GameObject("RopeDemoRoot");
        TrackData[] tracks =
        {
            CreateTrack(
                root.transform,
                0,
                RopeProjectionDirection.Front,
                ropeMaterial),
            CreateTrack(
                root.transform,
                1,
                RopeProjectionDirection.Right,
                ropeMaterial),
            CreateTrack(
                root.transform,
                2,
                RopeProjectionDirection.Back,
                ropeMaterial),
            CreateTrack(
                root.transform,
                3,
                RopeProjectionDirection.Left,
                ropeMaterial)
        };

        CreateRoads();
        CreateTrackContent(
            tracks[0],
            PlatformMoveMode.Wander,
            false,
            false,
            platformMaterial,
            plateMaterial);
        CreateTrackContent(
            tracks[1],
            PlatformMoveMode.PressTrigger,
            false,
            false,
            platformMaterial,
            plateMaterial);
        CreateTrackContent(
            tracks[2],
            PlatformMoveMode.Wander,
            true,
            false,
            platformMaterial,
            plateMaterial);
        GameObject playerObject = CreatePlayer(playerMaterial);
        CameraFollowController follow =
            CreateCamera(playerObject.transform);
        PlayerController player =
            playerObject.GetComponent<PlayerController>();
        RopePathNetwork[] networks =
        {
            tracks[0].Network,
            tracks[1].Network,
            tracks[2].Network,
            tracks[3].Network
        };
        SerializedObject serializedPlayer =
            new SerializedObject(player);
        serializedPlayer.FindProperty("cameraFollow")
            .objectReferenceValue = follow;
        SerializedProperty serializedNetworks =
            serializedPlayer.FindProperty("ropeNetworks");
        serializedNetworks.arraySize = networks.Length;
        for (int index = 0; index < networks.Length; index++)
        {
            serializedNetworks.GetArrayElementAtIndex(index)
                .objectReferenceValue = networks[index];
        }
        serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static TrackData CreateTrack(
        Transform parent,
        int index,
        RopeProjectionDirection direction,
        Material ropeMaterial)
    {
        GameObject networkObject =
            new GameObject($"Track_{index + 1}_{direction}_Network");
        networkObject.transform.SetParent(parent, false);
        RopePathNetwork network =
            networkObject.AddComponent<RopePathNetwork>();
        network.ConnectionTolerance = 0.08f;
        network.CurrentProjectionDirection = direction;

        Vector3 screenRight =
            RopeProjectionUtility.ScreenRight(direction);
        Vector3 viewDepth =
            RopeProjectionUtility.ViewDepth(direction);
        Vector3[] localPoints =
        {
            new Vector3(5f, 0f, 0f),
            new Vector3(8f, 0f, 0f),
            new Vector3(8f, 0f, 2.5f),
            new Vector3(8f, 2.5f, 2.5f),
            new Vector3(8f, 2.5f, -3f),
            new Vector3(12f, 2.5f, -3f),
            new Vector3(12f, 2.5f, 3f),
            new Vector3(12f, 0f, 3f),
            new Vector3(12f, 0f, -2.5f),
            new Vector3(16f, 0f, -2.5f),
            new Vector3(16f, 0f, 3f),
            new Vector3(16f, 2.5f, 3f),
            new Vector3(16f, 2.5f, -2f),
            new Vector3(20f, 2.5f, -2f),
            new Vector3(20f, 2.5f, 3f),
            new Vector3(20f, 0f, 3f),
            new Vector3(20f, 0f, -2f),
            new Vector3(24f, 0f, -2f),
            new Vector3(24f, 0f, 3f),
            new Vector3(27f, 0f, 3f),
            new Vector3(27f, 0f, 0f),
            new Vector3(30f, 0f, 0f)
        };

        List<RopeSegment> segments = new List<RopeSegment>();
        for (int pointIndex = 0;
             pointIndex < localPoints.Length - 1;
             pointIndex += 2)
        {
            Vector3 worldA = ToWorld(
                localPoints[pointIndex],
                screenRight,
                viewDepth);
            Vector3 worldB = ToWorld(
                localPoints[pointIndex + 1],
                screenRight,
                viewDepth);
            GameObject segmentObject = new GameObject(
                $"Track_{index + 1}_Rope_{pointIndex / 2 + 1:00}");
            segmentObject.transform.SetParent(
                networkObject.transform,
                false);
            RopeSegment segment =
                segmentObject.AddComponent<RopeSegment>();
            segment.SetLocalEndpoint(RopeEndpoint.A, worldA);
            segment.SetLocalEndpoint(RopeEndpoint.B, worldB);
            segment.EnsureVisuals(ropeMaterial);
            segments.Add(segment);
        }

        return new TrackData
        {
            Index = index,
            Name = $"Track_{index + 1}",
            Network = network,
            Segments = segments,
            Platforms = new List<PlatformMove>(),
            Forward = screenRight
        };
    }

    private static Vector3 ToWorld(
        Vector3 local,
        Vector3 screenRight,
        Vector3 viewDepth)
    {
        return screenRight * local.x +
               Vector3.up * local.y +
               viewDepth * local.z;
    }

    private static void CreateTrackContent(
        TrackData track,
        PlatformMoveMode mode,
        bool useButtonFeature,
        bool useFivePlatforms,
        Material platformMaterial,
        Material plateMaterial)
    {
        int platformCount = useFivePlatforms ? 5 : 1;
        for (int index = 0;
             index < platformCount;
             index++)
        {
            int segmentIndex = useFivePlatforms
                ? Mathf.Min(index * 2, track.Segments.Count - 1)
                : 0;
            PlatformMove platform = CreatePlatform(
                track,
                track.Segments[segmentIndex],
                RopeEndpoint.A,
                mode,
                useButtonFeature,
                platformMaterial);
            track.Platforms.Add(platform);
        }

        if (!useButtonFeature)
        {
            return;
        }

        GameObject plateObject = CreatePressurePlate(
            track.Name + "_PressurePlate",
            track.Forward * 3f,
            track.Platforms.ToArray(),
            plateMaterial);
        plateObject.transform.SetParent(
            track.Network.transform,
            true);
    }

    private static PlatformMove CreatePlatform(
        TrackData track,
        RopeSegment segment,
        RopeEndpoint endpoint,
        PlatformMoveMode mode,
        bool useButtonFeature,
        Material material)
    {
        GameObject platformObject =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        platformObject.name =
            $"{track.Name}_Platform_{track.Platforms.Count + 1:00}";
        platformObject.transform.SetParent(
            track.Network.transform,
            false);
        platformObject.transform.localScale =
            new Vector3(0.9f, 0.25f, 0.9f);
        ApplyMaterial(platformObject, material);
        RopePlatform ropePlatform =
            platformObject.AddComponent<RopePlatform>();
        PlatformMove platformMove =
            platformObject.GetComponent<PlatformMove>();
        SerializedObject serialized =
            new SerializedObject(platformMove);
        serialized.FindProperty("network")
            .objectReferenceValue = track.Network;
        serialized.FindProperty("moveMode")
            .enumValueIndex = (int)mode;
        serialized.FindProperty("useButtonFeature")
            .boolValue = useButtonFeature;
        serialized.FindProperty("buttonWaitDuration")
            .floatValue = 5f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        ropePlatform.Bind(track.Network, segment, endpoint);
        ropePlatform.SnapTransformToBinding();
        return platformMove;
    }

    private static GameObject CreatePressurePlate(
        string name,
        Vector3 position,
        PlatformMove[] platforms,
        Material material)
    {
        GameObject plateObject =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        plateObject.name = name;
        plateObject.transform.position = position;
        plateObject.transform.localScale =
            new Vector3(2.2f, 0.2f, 2.2f);
        ApplyMaterial(plateObject, material);
        PressurePlate plate =
            plateObject.AddComponent<PressurePlate>();
        SerializedObject serialized =
            new SerializedObject(plate);
        SerializedProperty platformArray =
            serialized.FindProperty("commandedPlatforms");
        platformArray.arraySize = platforms.Length;
        for (int index = 0;
             index < platforms.Length;
             index++)
        {
            platformArray.GetArrayElementAtIndex(index)
                .objectReferenceValue = platforms[index];
        }

        serialized.FindProperty("playerTag")
            .stringValue = "Player";
        serialized.FindProperty("checkHeight")
            .floatValue = 0.8f;
        serialized.FindProperty("checkWidth")
            .floatValue = 0.9f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return plateObject;
    }

    private static void CreateRoads()
    {
        CreateCube(
            "CenterHub",
            new Vector3(0f, -0.25f, 0f),
            new Vector3(9f, 0.5f, 9f));
        CreateCube(
            "OuterRoad_East",
            new Vector3(30f, -0.25f, 0f),
            new Vector3(3f, 0.5f, 60f));
        CreateCube(
            "OuterRoad_West",
            new Vector3(-30f, -0.25f, 0f),
            new Vector3(3f, 0.5f, 60f));
        CreateCube(
            "OuterRoad_North",
            new Vector3(0f, -0.25f, 30f),
            new Vector3(60f, 0.5f, 3f));
        CreateCube(
            "OuterRoad_South",
            new Vector3(0f, -0.25f, -30f),
            new Vector3(60f, 0.5f, 3f));
    }

    private static GameObject CreatePlayer(Material material)
    {
        GameObject playerObject =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        playerObject.name = "DemoPlayer";
        playerObject.tag = "Player";
        playerObject.transform.position =
            new Vector3(0f, 1f, 0f);
        playerObject.transform.localScale =
            Vector3.one * 0.8f;
        ApplyMaterial(playerObject, material);
        BoxCollider detectionCollider =
            playerObject.GetComponent<BoxCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.center = Vector3.zero;
        detectionCollider.size = new Vector3(1f, 2.2f, 1f);
        Rigidbody body = playerObject.AddComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.collisionDetectionMode =
            CollisionDetectionMode.Continuous;
        CapsuleCollider capsule =
            playerObject.AddComponent<CapsuleCollider>();
        capsule.radius = 0.5f;
        capsule.height = 1f;
        capsule.center = Vector3.zero;
        playerObject.AddComponent<PlayableDirector>();
        playerObject.AddComponent<PlayerActionRunner>();
        TimelineActorHost actorHost =
            playerObject.AddComponent<TimelineActorHost>();
        actorHost.attackPoint = playerObject.transform;
        PlayerController controller =
            playerObject.AddComponent<PlayerController>();
        playerObject.AddComponent<PlatformRider>();
        return playerObject;
    }

    private static CameraFollowController CreateCamera(
        Transform target)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        camera.orthographic = true;
        camera.orthographicSize = 17f;
        CameraControlManager manager =
            cameraObject.AddComponent<CameraControlManager>();
        CameraModeController modeController =
            cameraObject.AddComponent<CameraModeController>();
        CameraFollowController follow =
            cameraObject.AddComponent<CameraFollowController>();
        modeController.Configure(camera, target, true);
        follow.Configure(manager, modeController, target);
        follow.SetRequestedMode(
            CameraViewMode.Side2D,
            0f,
            true);
        return follow;
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightObject.transform.rotation =
            Quaternion.Euler(50f, -30f, 0f);
    }

    private static GameObject CreateCube(
        string name,
        Vector3 position,
        Vector3 scale)
    {
        GameObject gameObject =
            GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.position = position;
        gameObject.transform.localScale = scale;
        return gameObject;
    }

    private static void ApplyMaterial(
        GameObject gameObject,
        Material material)
    {
        if (gameObject == null || material == null)
        {
            return;
        }

        MeshRenderer renderer =
            gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }
    }

    private static Material GetOrCreateMaterial(
        string fileName,
        Color color)
    {
        EnsureFolder(MaterialFolder);
        string path = MaterialFolder + "/" + fileName;
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Standard");
        if (shader == null)
        {
            return null;
        }

        material = new Material(shader)
        {
            name = fileName.Replace(".mat", string.Empty)
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor(
                "_EmissionColor",
                color * 0.35f);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[index]);
            }

            current = next;
        }
    }
}
