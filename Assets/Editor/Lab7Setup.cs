using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Pathfinding;

public static class Lab7Setup
{
    // Note: Automatic execution disabled so user's manual scene adjustments are never overwritten!

    [MenuItem("Tools/Setup Lab 7 Pathfinding")]
    public static void ExecuteSetup()
    {
        Debug.Log("[Lab7Setup] Starting setup for Lab 7 Tile-by-Tile Grid Movement with 4 Enemies...");

        // 1. Configure Sprites
        SetupSpriteImporter("Assets/Sprites/Maps/Rocket_Hideout_B2F.png", 16);
        SetupSpriteImporter("Assets/Sprites/Square.png", 32);
        SetupSpriteImporter("Assets/Sprites/Player.png", 100);
        if (System.IO.File.Exists("Assets/Assets/Player.png"))
        {
            SetupSpriteImporter("Assets/Assets/Player.png", 100);
        }

        AssetDatabase.Refresh();

        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Maps/Rocket_Hideout_B2F.png");
        Sprite redSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Red/red_down_idle.png");
        Sprite playerSprite = redSprite != null ? redSprite : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Player.png");

        // 2. Open / Prepare Scene
        string scenePath = "Assets/Scenes/SampleScene.unity";
        Scene scene;
        if (System.IO.File.Exists(scenePath))
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        // 3. Configure Camera (Authentic GBA handheld zoom & follow)
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            mainCam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
        }
        mainCam.transform.position = new Vector3(7.0f, 5.5f, -10f);
        mainCam.orthographic = true;
        mainCam.orthographicSize = 5.0f; // Authentic GBA 10-tile vertical zoom
        mainCam.clearFlags = CameraClearFlags.SolidColor;
        mainCam.backgroundColor = Color.black;

        CameraFollow camFollow = mainCam.GetComponent<CameraFollow>();
        if (camFollow == null)
        {
            camFollow = mainCam.gameObject.AddComponent<CameraFollow>();
        }
        camFollow.smoothSpeed = 12f;
        camFollow.smoothFollow = true;
        camFollow.clampToBounds = true;
        camFollow.minBounds = new Vector2(-15.5f, -11.0f);
        camFollow.maxBounds = new Vector2(15.5f, 11.0f);

        // Clean existing lab objects to avoid duplicates
        DestroyIfExists("B2F_Map");
        DestroyIfExists("Environment");
        DestroyIfExists("Player");
        DestroyIfExists("Enemy");
        DestroyIfExists("Enemy_AquaMale");
        DestroyIfExists("Enemy_AquaFemale");
        DestroyIfExists("Enemy_MagmaMale");
        DestroyIfExists("Enemy_MagmaFemale");
        DestroyIfExists("SpinTiles");
        DestroyIfExists("PathFinding");

        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer == -1)
        {
            envLayer = 8;
        }

        // 4. Create B2F Map Object
        GameObject mapGo = new GameObject("B2F_Map");
        mapGo.transform.position = Vector3.zero;
        mapGo.transform.localScale = Vector3.one;

        SpriteRenderer mapSr = mapGo.AddComponent<SpriteRenderer>();
        mapSr.sprite = mapSprite;
        mapSr.sortingOrder = 0;

        // 5. Create Environment with B2F Merged Colliders
        GameObject env = new GameObject("Environment");
        env.transform.position = Vector3.zero;
        env.layer = envLayer;

        CreateB2FColliders(env.transform, envLayer);

        // 5b. Create SpinTiles Container (42 Arrow Pads + 12 Stop Pads)
        GameObject spinTilesGo = new GameObject("SpinTiles");
        spinTilesGo.transform.position = Vector3.zero;
        CreateSpinPads(spinTilesGo.transform);

        // 6. Create Player
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(7.0f, 5.5f, 0f);
        player.tag = "Player";

        SpriteRenderer playerSr = player.AddComponent<SpriteRenderer>();
        playerSr.sprite = playerSprite;
        playerSr.sortingOrder = 2;

        CircleCollider2D playerCol = player.AddComponent<CircleCollider2D>();
        playerCol.radius = 0.35f;
        playerCol.offset = new Vector2(0f, -0.55f);

        Rigidbody2D playerRb = player.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 0f;
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PlayerScript playerScript = player.AddComponent<PlayerScript>();
        playerScript.Speed = 5f;

        camFollow.target = player.transform;
        camFollow.mapRenderer = mapSr;

        // 7. Create All 4 Enemy Types (Strictly Tile-Locked with Pac-Man Personalities)
        CreateEnemy("Enemy_AquaMale", new Vector3(-11.0f, 6.5f, 0f), EnemyMovement.EnemyType.AquaMale, EnemyAI.Personality.Inky_Flanker, "Assets/Sprites/Enemies/Aqua_Male/down_idle.png", player.transform);
        CreateEnemy("Enemy_AquaFemale", new Vector3(-4.0f, -0.5f, 0f), EnemyMovement.EnemyType.AquaFemale, EnemyAI.Personality.Pinky_Ambusher, "Assets/Sprites/Enemies/Aqua_Female/down_idle.png", player.transform);
        CreateEnemy("Enemy_MagmaMale", new Vector3(-11.0f, -5.5f, 0f), EnemyMovement.EnemyType.MagmaMale, EnemyAI.Personality.Blinky_Direct, "Assets/Sprites/Enemies/Magma_Male/down_idle.png", player.transform);
        CreateEnemy("Enemy_MagmaFemale", new Vector3(5.0f, -6.5f, 0f), EnemyMovement.EnemyType.MagmaFemale, EnemyAI.Personality.Clyde_Wanderer, "Assets/Sprites/Enemies/Magma_Female/down_idle.png", player.transform);

        // 8. Create GridManager for strictly Tile-by-Tile navigation (Zero Astar Zip Bugs)
        DestroyIfExists("GridManager");
        GameObject gridMgrGo = new GameObject("GridManager");
        gridMgrGo.transform.position = Vector3.zero;
        GridManager gridMgr = gridMgrGo.AddComponent<GridManager>();
        gridMgr.minX = -14f;
        gridMgr.maxX = 14f;
        gridMgr.minY = -9.5f;
        gridMgr.maxY = 8.5f;
        gridMgr.obstacleLayer = 1 << envLayer;

        EditorUtility.SetDirty(gridMgr);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        Debug.Log("[Lab7Setup] Successfully configured 4-directional Lab 7 scene with all 4 enemies!");
    }

    private static GameObject CreateEnemy(string name, Vector3 pos, EnemyMovement.EnemyType type, EnemyAI.Personality personality, string spritePath, Transform target)
    {
        pos.x = Mathf.Round(pos.x);
        pos.y = Mathf.Floor(pos.y) + 0.5f;

        GameObject enemy = new GameObject(name);
        enemy.transform.position = pos;
        enemy.tag = "Enemy";

        Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        SpriteRenderer enemySr = enemy.AddComponent<SpriteRenderer>();
        enemySr.sprite = enemySprite;
        enemySr.sortingOrder = 2;

        CircleCollider2D enemyCol = enemy.AddComponent<CircleCollider2D>();
        enemyCol.offset = Vector2.zero;
        enemyCol.radius = 0.35f;
        enemyCol.isTrigger = false;

        Rigidbody2D enemyRb = enemy.AddComponent<Rigidbody2D>();
        enemyRb.gravityScale = 0f;
        enemyRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        enemyRb.bodyType = RigidbodyType2D.Kinematic;

        EnemyMovement enemyMovement = enemy.AddComponent<EnemyMovement>();
        enemyMovement.enemyType = type;
        enemyMovement.speed = 2.8f;
        enemyMovement.spriteRenderer = enemySr;

        EnemyAI enemyAI = enemy.AddComponent<EnemyAI>();
        enemyAI.personality = personality;
        enemyAI.target = target;
        enemyAI.stepDelay = 0.05f;

        return enemy;
    }

    private static void CreateB2FColliders(Transform parent, int envLayer)
    {
        CreateCollider(parent, "Wall (0)", new Vector2(0.00f, 10.00f), new Vector2(31.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (1)", new Vector2(-15.00f, -1.00f), new Vector2(1.00f, 20.00f), envLayer);
        CreateCollider(parent, "Wall (2)", new Vector2(-10.00f, 8.00f), new Vector2(1.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (3)", new Vector2(1.50f, 8.50f), new Vector2(4.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (4)", new Vector2(9.00f, 8.00f), new Vector2(5.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (5)", new Vector2(14.00f, 8.50f), new Vector2(3.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (6)", new Vector2(-13.00f, 7.50f), new Vector2(3.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (7)", new Vector2(-8.00f, 7.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (8)", new Vector2(-6.00f, 7.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (9)", new Vector2(-4.00f, 7.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (10)", new Vector2(-2.00f, 7.00f), new Vector2(1.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (11)", new Vector2(2.00f, 7.00f), new Vector2(3.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (12)", new Vector2(14.50f, 7.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (13)", new Vector2(-0.50f, 6.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (14)", new Vector2(10.00f, 6.50f), new Vector2(3.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (15)", new Vector2(-14.00f, 4.50f), new Vector2(1.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (16)", new Vector2(-9.00f, 5.50f), new Vector2(5.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (17)", new Vector2(-5.00f, 4.00f), new Vector2(1.00f, 4.00f), envLayer);
        CreateCollider(parent, "Wall (18)", new Vector2(11.00f, 5.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (19)", new Vector2(-11.50f, 3.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (20)", new Vector2(-9.00f, 3.00f), new Vector2(1.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (21)", new Vector2(-7.00f, 3.00f), new Vector2(1.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (22)", new Vector2(0.00f, 3.50f), new Vector2(3.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (23)", new Vector2(-11.00f, 2.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (24)", new Vector2(-1.00f, 2.00f), new Vector2(1.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (25)", new Vector2(-13.50f, 1.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (26)", new Vector2(-3.00f, 1.50f), new Vector2(3.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (27)", new Vector2(8.00f, 0.50f), new Vector2(15.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (28)", new Vector2(-3.50f, 0.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (29)", new Vector2(-13.50f, -0.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (30)", new Vector2(-10.00f, -1.00f), new Vector2(3.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (31)", new Vector2(-7.50f, -1.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (32)", new Vector2(2.50f, -1.50f), new Vector2(4.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (33)", new Vector2(11.50f, -1.50f), new Vector2(8.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (34)", new Vector2(-13.00f, -2.50f), new Vector2(3.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (35)", new Vector2(-8.00f, -2.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (36)", new Vector2(2.00f, -3.00f), new Vector2(3.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (37)", new Vector2(12.00f, -2.50f), new Vector2(7.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (38)", new Vector2(-1.50f, -3.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (39)", new Vector2(13.00f, -4.50f), new Vector2(5.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (40)", new Vector2(-13.50f, -4.50f), new Vector2(2.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (41)", new Vector2(-11.00f, -4.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (42)", new Vector2(-9.00f, -7.50f), new Vector2(1.00f, 7.00f), envLayer);
        CreateCollider(parent, "Wall (43)", new Vector2(-7.00f, -5.00f), new Vector2(1.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (44)", new Vector2(-1.00f, -6.50f), new Vector2(1.00f, 5.00f), envLayer);
        CreateCollider(parent, "Wall (45)", new Vector2(-14.00f, -5.50f), new Vector2(1.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (46)", new Vector2(-4.50f, -5.50f), new Vector2(4.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (47)", new Vector2(-13.00f, -7.50f), new Vector2(1.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (48)", new Vector2(-11.00f, -7.50f), new Vector2(1.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (49)", new Vector2(2.50f, -8.50f), new Vector2(2.00f, 5.00f), envLayer);
        CreateCollider(parent, "Wall (50)", new Vector2(-3.50f, -8.50f), new Vector2(4.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (51)", new Vector2(1.00f, -9.50f), new Vector2(1.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (52)", new Vector2(9.50f, -9.50f), new Vector2(12.00f, 3.00f), envLayer);
        CreateCollider(parent, "Wall (53)", new Vector2(-7.50f, -10.00f), new Vector2(2.00f, 2.00f), envLayer);
        CreateCollider(parent, "Wall (54)", new Vector2(-12.00f, -10.50f), new Vector2(5.00f, 1.00f), envLayer);
        CreateCollider(parent, "Wall (55)", new Vector2(-3.00f, -10.50f), new Vector2(7.00f, 1.00f), envLayer);
        CreateCollider(parent, "RightBoundary", new Vector2(16.00f, 0.00f), new Vector2(1.00f, 22.00f), envLayer);
    }

    private static void SetupSpriteImporter(string path, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.SaveAndReimport();
        }
    }

    private static GameObject CreateCollider(Transform parent, string name, Vector2 pos, Vector2 size, int layer)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.position = new Vector3(pos.x, pos.y, 0f);
        wall.transform.localScale = Vector3.one;
        wall.layer = layer;

        BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
        col.size = size;

        return wall;
    }

    private static void DestroyIfExists(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            GameObject.DestroyImmediate(go);
        }
    }

    private static void CreateSpinPads(Transform parent)
    {
        // 12 Stop Pads
        CreateStopPad(parent, "StopPad_1_4", new Vector3(-14.00f, 6.50f, 0f));
        CreateStopPad(parent, "StopPad_7_6", new Vector3(-8.00f, 4.50f, 0f));
        CreateStopPad(parent, "StopPad_13_7", new Vector3(-2.00f, 3.50f, 0f));
        CreateStopPad(parent, "StopPad_15_8", new Vector3(0.00f, 2.50f, 0f));
        CreateStopPad(parent, "StopPad_13_10", new Vector3(-2.00f, 0.50f, 0f));
        CreateStopPad(parent, "StopPad_8_11", new Vector3(-7.00f, -0.50f, 0f));
        CreateStopPad(parent, "StopPad_14_13", new Vector3(-1.00f, -2.50f, 0f));
        CreateStopPad(parent, "StopPad_1_14", new Vector3(-14.00f, -3.50f, 0f));
        CreateStopPad(parent, "StopPad_5_15", new Vector3(-10.00f, -4.50f, 0f));
        CreateStopPad(parent, "StopPad_10_15", new Vector3(-5.00f, -4.50f, 0f));
        CreateStopPad(parent, "StopPad_8_19", new Vector3(-7.00f, -8.50f, 0f));
        CreateStopPad(parent, "StopPad_13_20", new Vector3(-2.00f, -9.50f, 0f));

        // 42 Arrow Pads
        CreateArrowPad(parent, "ArrowPad_Left_3_4", new Vector3(-12.00f, 6.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Left_7_4", new Vector3(-8.00f, 6.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Left_9_4", new Vector3(-6.00f, 6.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Left_11_4", new Vector3(-4.00f, 6.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_9_5", new Vector3(-6.00f, 5.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Down_12_5", new Vector3(-3.00f, 5.50f, 0f), ArrowPad.Direction.Down);
        CreateArrowPad(parent, "ArrowPad_Left_16_5", new Vector3(1.00f, 5.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Right_3_6", new Vector3(-12.00f, 4.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Up_11_6", new Vector3(-4.00f, 4.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Left_16_6", new Vector3(1.00f, 4.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_7_7", new Vector3(-8.00f, 3.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Right_12_7", new Vector3(-3.00f, 3.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Up_11_8", new Vector3(-4.00f, 2.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Right_4_9", new Vector3(-11.00f, 1.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Down_8_9", new Vector3(-7.00f, 1.50f, 0f), ArrowPad.Direction.Down);
        CreateArrowPad(parent, "ArrowPad_Down_10_9", new Vector3(-5.00f, 1.50f, 0f), ArrowPad.Direction.Down);
        CreateArrowPad(parent, "ArrowPad_Up_15_9", new Vector3(0.00f, 1.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Right_3_10", new Vector3(-12.00f, 0.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Up_7_10", new Vector3(-8.00f, 0.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Up_9_10", new Vector3(-6.00f, 0.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Up_3_11", new Vector3(-12.00f, -0.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Right_10_11", new Vector3(-5.00f, -0.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Right_12_11", new Vector3(-3.00f, -0.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Down_14_11", new Vector3(-1.00f, -0.50f, 0f), ArrowPad.Direction.Down);
        CreateArrowPad(parent, "ArrowPad_Up_15_11", new Vector3(0.00f, -0.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Right_9_12", new Vector3(-6.00f, -1.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Right_11_12", new Vector3(-4.00f, -1.50f, 0f), ArrowPad.Direction.Right);
        CreateArrowPad(parent, "ArrowPad_Up_13_12", new Vector3(-2.00f, -1.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Down_10_13", new Vector3(-5.00f, -2.50f, 0f), ArrowPad.Direction.Down);
        CreateArrowPad(parent, "ArrowPad_Left_12_13", new Vector3(-3.00f, -2.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_15_13", new Vector3(0.00f, -2.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Left_3_14", new Vector3(-12.00f, -3.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Left_7_14", new Vector3(-8.00f, -3.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_9_14", new Vector3(-6.00f, -3.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Left_12_14", new Vector3(-3.00f, -3.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_5_17", new Vector3(-10.00f, -6.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Down_8_17", new Vector3(-7.00f, -6.50f, 0f), ArrowPad.Direction.Down);
        CreateArrowPad(parent, "ArrowPad_Left_12_17", new Vector3(-3.00f, -6.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_7_18", new Vector3(-8.00f, -7.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Left_12_18", new Vector3(-3.00f, -7.50f, 0f), ArrowPad.Direction.Left);
        CreateArrowPad(parent, "ArrowPad_Up_5_19", new Vector3(-10.00f, -8.50f, 0f), ArrowPad.Direction.Up);
        CreateArrowPad(parent, "ArrowPad_Right_9_20", new Vector3(-6.00f, -9.50f, 0f), ArrowPad.Direction.Right);
    }

    private static void CreateArrowPad(Transform parent, string name, Vector3 pos, ArrowPad.Direction dir)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.isTrigger = true;
        ArrowPad pad = go.AddComponent<ArrowPad>();
        pad.direction = dir;
    }

    private static void CreateStopPad(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.6f, 0.6f);
        col.isTrigger = true;
        go.AddComponent<StopPad>();
    }
}
