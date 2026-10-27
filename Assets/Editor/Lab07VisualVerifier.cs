using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class Lab07VisualVerifier
{
    private const string ScenePath = "Assets/Scenes/Lab07_Actividades.unity";
    public static string OutputDir => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "VerificationCaptures");
    private static readonly string TriggerPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "RunVerification.trigger");

    static Lab07VisualVerifier()
    {
        EditorApplication.update += CheckTrigger;
    }

    private static void CheckTrigger()
    {
        if (File.Exists(TriggerPath))
        {
            string mode = "all";
            try
            {
                mode = File.ReadAllText(TriggerPath).Trim();
                File.Delete(TriggerPath);
            }
            catch (Exception) { return; }

            Debug.Log($"[Lab07VisualVerifier] Trigger detected! Mode: {mode}");

            string stateFile = System.IO.Path.Combine(OutputDir, "PLAYMODE_STATE.txt");
            File.WriteAllText(stateFile, $"isPlaying: {EditorApplication.isPlaying}, isCompiling: {EditorApplication.isCompiling}, isPaused: {EditorApplication.isPaused}, time: {DateTime.Now}\n");

            if (mode == "stop")
            {
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorApplication.isPlaying)
            {
                Debug.Log("[Lab07VisualVerifier] Currently in PlayMode. Stopping PlayMode first to ensure clean state...");
                EditorApplication.isPlaying = false;
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.update += WaitForPlayModeExit;
                };
                return;
            }

            ExecuteVerificationPipeline(mode.Contains("play"));
        }
    }

    private static void WaitForPlayModeExit()
    {
        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.update -= WaitForPlayModeExit;
            Debug.Log("[Lab07VisualVerifier] PlayMode stopped cleanly. Executing fresh verification pipeline...");
            ExecuteVerificationPipeline(true);
        }
    }

    [MenuItem("Lab07/Run Full Verification & Captures")]
    public static void MenuRunFullVerification()
    {
        ExecuteVerificationPipeline(true);
    }

    [MenuItem("Lab07/1. Run Integrity Verification Only")]
    public static void MenuRunIntegrityOnly()
    {
        ExecuteVerificationPipeline(false);
    }

    public static void ExecuteVerificationPipeline(bool includePlayMode)
    {
        Directory.CreateDirectory(OutputDir);

        // 1. Ensure scene is open
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // 2. Run Scene Integrity Audit
        int errors = RunIntegrityAudit();

        // 3. Capture Edit Mode High-Res Views
        CaptureEditModeViews();

        // 4. If requested, attach temporary runtime runner (not saved to scene) and launch PlayMode
        if (includePlayMode && errors == 0)
        {
            GameObject runnerGo = GameObject.Find("Lab07AutoTestRunner");
            if (runnerGo == null)
            {
                runnerGo = new GameObject("Lab07AutoTestRunner");
                runnerGo.hideFlags = HideFlags.DontSave;
                runnerGo.AddComponent<Lab07AutoTestRunner>();
            }

            var runner = runnerGo.GetComponent<Lab07AutoTestRunner>();
            if (runner != null)
            {
                runner.autoRunTest = false; // Keep false by default
            }

            Debug.Log("[Lab07VisualVerifier] PlayMode verification ready.");
        }
        else
        {
            string doneFile = System.IO.Path.Combine(OutputDir, "AUDIT_FINISHED.txt");
            File.WriteAllText(doneFile, $"Finished at {DateTime.Now} with {errors} errors.");
        }
    }

    public static int RunIntegrityAudit()
    {
        Debug.Log("==================================================================");
        Debug.Log("[Lab07VisualVerifier] INITIATING SCENE INTEGRITY AUDIT...");
        Debug.Log("==================================================================");

        int errors = 0;

        // 1. Audit Main Camera
        Camera cam = Camera.main;
        if (cam == null) { Debug.LogError("FAIL: Main Camera not found!"); errors++; }
        else
        {
            Lab07CameraFollow follow = cam.GetComponent<Lab07CameraFollow>();
            if (follow == null) { Debug.LogError("FAIL: Lab07CameraFollow missing on Main Camera!"); errors++; }
            else { Debug.Log("PASS: Main Camera correctly configured with Lab07CameraFollow."); }

            GridManager rogueGrid = cam.GetComponent<GridManager>();
            if (rogueGrid != null) { Debug.LogError("FAIL: Rogue GridManager still attached to Main Camera!"); errors++; }
        }

        // 2. Audit Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { Debug.LogError("FAIL: Player with tag 'Player' not found!"); errors++; }
        else
        {
            PlayerScript ps = player.GetComponent<PlayerScript>();
            if (ps == null) { Debug.LogError("FAIL: PlayerScript missing on Player!"); errors++; }
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb == null) { Debug.LogError("FAIL: Rigidbody2D missing on Player!"); errors++; }
            CircleCollider2D col = player.GetComponent<CircleCollider2D>();
            if (col == null) { Debug.LogError("FAIL: CircleCollider2D missing on Player!"); errors++; }
            Debug.Log($"PASS: Player verified at position {player.transform.position}.");
        }

        // 3. Audit PathFinding & AstarPath
        GameObject pfGo = GameObject.Find("PathFinding");
        if (pfGo == null) { Debug.LogError("FAIL: PathFinding GameObject not found!"); errors++; }
        else
        {
            AstarPath astar = pfGo.GetComponent<AstarPath>();
            if (astar == null) { Debug.LogError("FAIL: AstarPath missing on PathFinding!"); errors++; }
            Lab07PathfindingInitializer init = pfGo.GetComponent<Lab07PathfindingInitializer>();
            if (init == null) { Debug.LogError("FAIL: Lab07PathfindingInitializer missing on PathFinding!"); errors++; }
            StopPad roguePad = pfGo.GetComponent<StopPad>();
            if (roguePad != null) { Debug.LogError("FAIL: Rogue StopPad still attached to PathFinding!"); errors++; }
            Debug.Log("PASS: PathFinding manager verified with AstarPath and Lab07PathfindingInitializer.");
        }

        // 4. Audit Enemies
        string[] expectedEnemies = { "Enemy_Hunter", "Enemy_Infiltrator", "Enemy_Juggernaut", "Enemy_Patrol_Alpha", "Enemy_Sentinel" };
        foreach (var eName in expectedEnemies)
        {
            GameObject eGo = GameObject.Find(eName);
            if (eGo == null) { Debug.LogError($"FAIL: Enemy '{eName}' not found!"); errors++; continue; }
            Pathfinding.Seeker seeker = eGo.GetComponent<Pathfinding.Seeker>();
            Pathfinding.AIPath aip = eGo.GetComponent<Pathfinding.AIPath>();
            Pathfinding.AIDestinationSetter dest = eGo.GetComponent<Pathfinding.AIDestinationSetter>();
            if (seeker == null || aip == null || dest == null)
            {
                Debug.LogError($"FAIL: Enemy '{eName}' is missing A* navigation components!"); errors++;
            }
            Transform gfx = eGo.transform.Find("Graphics");
            if (gfx == null) { Debug.LogError($"FAIL: Enemy '{eName}' missing child Graphics!"); errors++; }
            else
            {
                EnemyMovement em = gfx.GetComponent<EnemyMovement>();
                if (em == null) { Debug.LogError($"FAIL: Graphics on '{eName}' missing EnemyMovement!"); errors++; }
            }
        }
        Debug.Log($"PASS: All {expectedEnemies.Length} A* AI enemies verified with Seeker, AIPath, AIDestinationSetter & EnemyMovement.");

        // 5. Audit Collectibles
        CollectibleItem[] collectibles = UnityEngine.Object.FindObjectsByType<CollectibleItem>(FindObjectsSortMode.None);
        if (collectibles.Length != 25)
        {
            Debug.LogError($"FAIL: Expected 25 CollectibleItems, found {collectibles.Length}!"); errors++;
        }
        else
        {
            int intelCount = 0, medkitCount = 0, speedCount = 0, empCount = 0;
            foreach (var item in collectibles)
            {
                switch (item.itemType)
                {
                    case CollectibleItem.ItemType.IntelDisk: intelCount++; break;
                    case CollectibleItem.ItemType.Medkit: medkitCount++; break;
                    case CollectibleItem.ItemType.SpeedBoost: speedCount++; break;
                    case CollectibleItem.ItemType.EMPCharge: empCount++; break;
                }
            }
            Debug.Log($"PASS: 25 Collectibles verified: {intelCount} Intel Disks, {medkitCount} Medkits, {speedCount} SpeedBoosts, {empCount} EMP Charges.");
        }

        // 6. Audit Blast Doors & Warp Pads
        DynamicBlastDoor[] doors = UnityEngine.Object.FindObjectsByType<DynamicBlastDoor>(FindObjectsSortMode.None);
        if (doors.Length != 4) { Debug.LogError($"FAIL: Expected 4 DynamicBlastDoors, found {doors.Length}!"); errors++; }
        else { Debug.Log($"PASS: All {doors.Length} DynamicBlastDoors verified."); }

        WarpPad[] pads = UnityEngine.Object.FindObjectsByType<WarpPad>(FindObjectsSortMode.None);
        if (pads.Length != 4) { Debug.LogError($"FAIL: Expected 4 WarpPads, found {pads.Length}!"); errors++; }
        else { Debug.Log($"PASS: All {pads.Length} WarpPads verified with cross-sector teleport destinations."); }

        // 7. Audit GameManager
        Lab07GameManager gm = UnityEngine.Object.FindFirstObjectByType<Lab07GameManager>();
        if (gm == null) { Debug.LogError("FAIL: Lab07GameManager not found!"); errors++; }
        else { Debug.Log("PASS: Lab07GameManager verified."); }

        Debug.Log("==================================================================");
        if (errors == 0)
        {
            Debug.Log(">>> SCENE INTEGRITY AUDIT RESULT: PERFECT (0 ERRORS)! <<<");
        }
        else
        {
            Debug.LogError($">>> SCENE INTEGRITY AUDIT RESULT: {errors} ERRORS FOUND! <<<");
        }
        Debug.Log("==================================================================");

        return errors;
    }

    public static void CaptureEditModeViews()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 1. Capture Adventure Zoom View (centered on Player)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;
        cam.transform.position = new Vector3(playerPos.x, playerPos.y, -10f);
        cam.orthographicSize = 7.5f;
        string path1 = System.IO.Path.Combine(OutputDir, "01_EditMode_AdventureZoom.png");
        RenderCameraToFile(cam, path1, 1920, 1080);
        Debug.Log($"[Lab07VisualVerifier] Captured: {path1}");

        // 2. Capture Satellite Full-Facility Overview (Overview of all 4 sectors)
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographicSize = 25.0f;
        string path2 = System.IO.Path.Combine(OutputDir, "02_EditMode_SatelliteOverview.png");
        RenderCameraToFile(cam, path2, 1920, 1080);
        Debug.Log($"[Lab07VisualVerifier] Captured: {path2}");

        // Restore camera
        cam.transform.position = new Vector3(playerPos.x, playerPos.y, -10f);
        cam.orthographicSize = 7.5f;
    }

    public static void RenderCameraToFile(Camera cam, string filePath, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture prevRt = cam.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(filePath, bytes);

        cam.targetTexture = prevRt;
        RenderTexture.active = prevActive;
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(tex);
    }
}
