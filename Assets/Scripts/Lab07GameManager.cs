using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pathfinding;

public class Lab07GameManager : MonoBehaviour
{
    public static Lab07GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        Won,
        GameOver
    }

    [Header("Game State")]
    public GameState currentState = GameState.Playing;
    public int playerLives = 3;
    public int maxLives = 3;
    public int collectedIntel = 0;
    public int totalIntel = 8;
    public int currentScore = 0;
    public float gameTimer = 0f;

    [Header("Abilities & Buffs")]
    public int empCharges = 1;
    public float empStunDuration = 3.5f;
    public float empStunRemaining = 0f;
    public float speedBoostRemaining = 0f;
    public float invincibilityTimer = 0f;

    [Header("Debug & HUD")]
    public bool showDebugHUD = true;

    private PlayerScript playerScript;
    private AIPath[] enemies;
    private DynamicBlastDoor[] doors;
    private AstarPathVisualizer pathVisualizer;
    private float defaultPlayerSpeed = 5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Find player
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            playerScript = pObj.GetComponent<PlayerScript>();
            if (playerScript != null)
            {
                defaultPlayerSpeed = playerScript.Speed;
            }
        }

        // Cache enemies, doors, items
        enemies = FindObjectsByType<AIPath>(FindObjectsSortMode.None);
        doors = FindObjectsByType<DynamicBlastDoor>(FindObjectsSortMode.None);
        pathVisualizer = FindFirstObjectByType<AstarPathVisualizer>();

        CollectibleItem[] items = FindObjectsByType<CollectibleItem>(FindObjectsSortMode.None);
        int intelCount = 0;
        foreach (var item in items)
        {
            if (item.itemType == CollectibleItem.ItemType.IntelDisk) intelCount++;
        }
        totalIntel = intelCount > 0 ? intelCount : 8;

        // Ensure modern Tactical Infiltration HUD is active
        if (GetComponent<Lab07TacticalHUD>() == null && FindFirstObjectByType<Lab07TacticalHUD>() == null)
        {
            gameObject.AddComponent<Lab07TacticalHUD>();
        }
    }

    void Update()
    {
        if (currentState == GameState.Playing)
        {
            gameTimer += Time.deltaTime;

            // EMP Countdown
            if (empStunRemaining > 0f)
            {
                empStunRemaining -= Time.deltaTime;
                if (empStunRemaining <= 0f)
                {
                    RestoreEnemyMovement();
                }
            }

            // Speed Boost Countdown
            if (speedBoostRemaining > 0f)
            {
                speedBoostRemaining -= Time.deltaTime;
                if (playerScript != null)
                {
                    playerScript.Speed = defaultPlayerSpeed * 1.6f;
                }
            }
            else
            {
                if (playerScript != null)
                {
                    playerScript.Speed = defaultPlayerSpeed;
                }
            }

            // Invincibility countdown
            if (invincibilityTimer > 0f)
            {
                invincibilityTimer -= Time.deltaTime;
            }

            // EMP Key Trigger
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TriggerEMP();
            }

            // Check enemy proximity collision for damage
            CheckEnemyPlayerDamage();
        }

        // Toggle Debug HUD
        if (Input.GetKeyDown(KeyCode.H))
        {
            showDebugHUD = !showDebugHUD;
        }

        // Restart Key
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void OnItemCollected(CollectibleItem item)
    {
        if (currentState != GameState.Playing) return;

        currentScore += item.scoreValue;

        switch (item.itemType)
        {
            case CollectibleItem.ItemType.IntelDisk:
                collectedIntel++;
                if (collectedIntel >= totalIntel)
                {
                    TriggerVictory();
                }
                break;
            case CollectibleItem.ItemType.SpeedBoost:
                speedBoostRemaining = 5.0f;
                break;
            case CollectibleItem.ItemType.EMPCharge:
                empCharges++;
                break;
            case CollectibleItem.ItemType.Medkit:
                playerLives = Mathf.Min(maxLives, playerLives + 1);
                break;
        }
    }

    public void TriggerEMP()
    {
        if (empCharges <= 0 || empStunRemaining > 0f) return;

        empCharges--;
        empStunRemaining = empStunDuration;

        // Visual EMP shockwave pulse
        if (playerScript != null)
        {
            SpawnShockwave(playerScript.transform.position);
        }

        if (enemies == null || enemies.Length == 0)
        {
            enemies = FindObjectsByType<AIPath>(FindObjectsSortMode.None);
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.canMove = false;
            }
        }
    }

    private void SpawnShockwave(Vector3 center)
    {
        GameObject wave = new GameObject("EMPShockwaveEffect");
        wave.transform.position = center;
        SpriteRenderer sr = wave.AddComponent<SpriteRenderer>();
        Shader sh = Shader.Find("Custom/ShockwaveEMP");
        if (sh != null)
        {
            sr.material = new Material(sh);
        }
        sr.color = new Color(0.3f, 0.9f, 1f, 1f);
        sr.sortingOrder = 6;
        wave.AddComponent<ShockwaveExpander>();
    }

    private void RestoreEnemyMovement()
    {
        if (enemies == null) return;
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.canMove = true;
            }
        }
    }

    private void CheckEnemyPlayerDamage()
    {
        if (invincibilityTimer > 0f || playerScript == null) return;

        Vector2 playerPos = playerScript.transform.position;
        if (enemies == null) return;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.canMove) continue;

            float dist = Vector2.Distance(playerPos, enemy.transform.position);
            if (dist < 1.15f) // Enemy caught player!
            {
                TakeDamage();
                break;
            }
        }
    }

    public void TakeDamage()
    {
        if (invincibilityTimer > 0f) return;

        playerLives--;
        invincibilityTimer = 1.6f;

        if (playerLives <= 0)
        {
            playerLives = 0;
            currentState = GameState.GameOver;
            if (playerScript != null)
            {
                Rigidbody2D rb = playerScript.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private void TriggerVictory()
    {
        currentState = GameState.Won;
        currentScore += Mathf.Max(0, (int)((180f - gameTimer) * 10f)); // Time bonus
    }

    void OnGUI()
    {
        // If Lab07TacticalHUD is active, it renders the modern high-tech HUD.
        // Otherwise fallback to legacy DrawHUD().
        if (GetComponent<Lab07TacticalHUD>() == null && FindFirstObjectByType<Lab07TacticalHUD>() == null)
        {
            DrawHUD();
        }

        if (currentState == GameState.Won)
        {
            DrawVictoryScreen();
        }
        else if (currentState == GameState.GameOver)
        {
            DrawGameOverScreen();
        }
    }

    private void DrawHUD()
    {
        GUIStyle panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTex(2, 2, new Color(0.08f, 0.12f, 0.18f, 0.88f));

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 15;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = Color.white;

        // 1. Top-Left Mission Panel
        GUILayout.BeginArea(new Rect(15, 15, 310, 135), panelStyle);
        GUILayout.Space(6);
        GUILayout.Label("  OPERACIÓN PATHFINDING - LAB 07", labelStyle);

        GUIStyle statStyle = new GUIStyle(GUI.skin.label);
        statStyle.fontSize = 13;
        statStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);

        GUILayout.Label($"  Intel Clasificado: {collectedIntel} / {totalIntel}", statStyle);

        string radarInfo = GetNearestIntelInfo();
        GUIStyle radarStyle = new GUIStyle(statStyle);
        radarStyle.normal.textColor = new Color(0.3f, 0.95f, 1f);
        GUILayout.Label($"  {radarInfo}", radarStyle);

        string hearts = "";
        for (int i = 0; i < maxLives; i++)
        {
            hearts += (i < playerLives) ? " ♥" : " ♡";
        }
        GUIStyle heartStyle = new GUIStyle(statStyle);
        heartStyle.normal.textColor = playerLives > 1 ? new Color(1f, 0.3f, 0.3f) : Color.red;
        GUILayout.Label($"  Vidas: {hearts}  |  Score: {currentScore}", heartStyle);

        int minutes = (int)(gameTimer / 60);
        int seconds = (int)(gameTimer % 60);
        GUILayout.Label($"  Tiempo: {minutes:D2}:{seconds:D2}", statStyle);
        GUILayout.EndArea();

        // 2. Top-Right Abilities Panel
        GUILayout.BeginArea(new Rect(Screen.width - 285, 15, 270, 115), panelStyle);
        GUILayout.Space(6);
        GUIStyle empStyle = new GUIStyle(labelStyle);
        if (empStunRemaining > 0f)
        {
            empStyle.normal.textColor = new Color(0.3f, 0.9f, 1f);
            GUILayout.Label($"  ⚡ EMP ACTIVO: {empStunRemaining:F1}s", empStyle);
        }
        else if (empCharges > 0)
        {
            empStyle.normal.textColor = new Color(0.4f, 1f, 0.4f);
            GUILayout.Label($"  ⚡ [ESPACIO] EMP LISTO ({empCharges})", empStyle);
        }
        else
        {
            empStyle.normal.textColor = Color.gray;
            GUILayout.Label("  ⚡ EMP: Agotado", empStyle);
        }

        if (speedBoostRemaining > 0f)
        {
            GUIStyle speedStyle = new GUIStyle(labelStyle);
            speedStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label($"  ⏩ SPRINT BOOST: {speedBoostRemaining:F1}s", speedStyle);
        }

        GUILayout.Label("  [Z] Cámara: Zoom / Satélite", statStyle);
        GUILayout.Label("  [E] Abrir / Cerrar Puertas", statStyle);
        GUILayout.Label("  WASD / Flechas: Moverse", statStyle);
        GUILayout.EndArea();

        // 3. Near-Door Notification
        bool nearDoor = false;
        if (doors != null)
        {
            foreach (var d in doors)
            {
                if (d != null && d.IsPlayerNear())
                {
                    nearDoor = true;
                    break;
                }
            }
        }

        if (nearDoor)
        {
            GUIStyle doorAlert = new GUIStyle(GUI.skin.box);
            doorAlert.normal.background = MakeTex(2, 2, new Color(0.9f, 0.4f, 0f, 0.9f));
            doorAlert.fontSize = 14;
            doorAlert.fontStyle = FontStyle.Bold;
            doorAlert.normal.textColor = Color.white;
            doorAlert.alignment = TextAnchor.MiddleCenter;
            GUI.Box(new Rect(Screen.width / 2 - 130, 20, 260, 32), "Presiona [E] para Puerta", doorAlert);
        }

        // 4. A* Pathfinding Info Box (Ejercicio 2)
        if (showDebugHUD && pathVisualizer != null)
        {
            GUILayout.BeginArea(new Rect(15, Screen.height - 130, 340, 115), panelStyle);
            GUILayout.Space(5);
            GUIStyle astarHeader = new GUIStyle(labelStyle);
            astarHeader.fontSize = 13;
            astarHeader.normal.textColor = new Color(1f, 0.65f, 0.2f);
            GUILayout.Label("  A* ALGORITHM INSPECTOR (Ejercicio II)", astarHeader);

            GUIStyle astarSub = new GUIStyle(statStyle);
            astarSub.fontSize = 11;
            GUILayout.Label($"  Función Evaluación: f(n) = g(n) + h(n)", astarSub);
            GUILayout.Label($"  Waypoints: {pathVisualizer.waypointCount}  |  Distancia g(n): {pathVisualizer.calculatedPathDistance:F1}m", astarSub);
            GUILayout.Label($"  Heurística h(n): {pathVisualizer.directEuclideanDistance:F1}m  |  Total f(n): {pathVisualizer.evaluationFunctionF:F1}", astarSub);
            GUILayout.Label("  Presiona [H] para ocultar/mostrar panel", astarSub);
            GUILayout.EndArea();
        }
    }

    private void DrawVictoryScreen()
    {
        GUIStyle modal = new GUIStyle(GUI.skin.box);
        modal.normal.background = MakeTex(2, 2, new Color(0.04f, 0.2f, 0.1f, 0.95f));

        float w = 450;
        float h = 220;
        Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        GUI.Box(rect, "", modal);

        GUILayout.BeginArea(rect);
        GUILayout.Space(25);
        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontSize = 24;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = new Color(0.4f, 1f, 0.4f);
        title.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("¡MISIÓN CUMPLIDA!", title);

        GUIStyle sub = new GUIStyle(GUI.skin.label);
        sub.fontSize = 15;
        sub.normal.textColor = Color.white;
        sub.alignment = TextAnchor.MiddleCenter;
        GUILayout.Space(10);
        GUILayout.Label($"Recuperaste todo el Intel en {(int)gameTimer}s\nPuntuación Final: {currentScore}", sub);

        GUILayout.Space(20);
        GUIStyle btn = new GUIStyle(GUI.skin.label);
        btn.fontSize = 14;
        btn.fontStyle = FontStyle.Bold;
        btn.normal.textColor = new Color(1f, 0.9f, 0.3f);
        btn.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Presiona [R] para reiniciar la simulación", btn);
        GUILayout.EndArea();
    }

    private void DrawGameOverScreen()
    {
        GUIStyle modal = new GUIStyle(GUI.skin.box);
        modal.normal.background = MakeTex(2, 2, new Color(0.25f, 0.05f, 0.05f, 0.95f));

        float w = 450;
        float h = 220;
        Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
        GUI.Box(rect, "", modal);

        GUILayout.BeginArea(rect);
        GUILayout.Space(25);
        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.fontSize = 24;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = new Color(1f, 0.3f, 0.3f);
        title.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("ALERTA: HAS SIDO CAPTURADO", title);

        GUIStyle sub = new GUIStyle(GUI.skin.label);
        sub.fontSize = 15;
        sub.normal.textColor = Color.white;
        sub.alignment = TextAnchor.MiddleCenter;
        GUILayout.Space(10);
        GUILayout.Label($"Los enemigos interceptaron tu ruta.\nIntel rescatado: {collectedIntel} / {totalIntel}", sub);

        GUILayout.Space(20);
        GUIStyle btn = new GUIStyle(GUI.skin.label);
        btn.fontSize = 14;
        btn.fontStyle = FontStyle.Bold;
        btn.normal.textColor = new Color(1f, 0.9f, 0.3f);
        btn.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Presiona [R] para reiniciar la simulación", btn);
        GUILayout.EndArea();
    }

    private string GetNearestIntelInfo()
    {
        if (playerScript == null) return "Localizando...";
        Vector2 playerPos = playerScript.transform.position;
        CollectibleItem[] items = FindObjectsByType<CollectibleItem>(FindObjectsSortMode.None);
        float closestDist = float.MaxValue;
        Vector2 closestDir = Vector2.zero;

        foreach (var item in items)
        {
            if (item != null && item.itemType == CollectibleItem.ItemType.IntelDisk)
            {
                float d = Vector2.Distance(playerPos, item.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    closestDir = (Vector2)item.transform.position - playerPos;
                }
            }
        }

        if (closestDist > 9999f || closestDist == float.MaxValue) return "¡Todos los Intels recogidos!";

        float angle = Mathf.Atan2(closestDir.y, closestDir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        string arrow = "→";
        string compass = "E";
        if (angle >= 22.5f && angle < 67.5f) { arrow = "↗"; compass = "NE"; }
        else if (angle >= 67.5f && angle < 112.5f) { arrow = "↑"; compass = "N"; }
        else if (angle >= 112.5f && angle < 157.5f) { arrow = "↖"; compass = "NW"; }
        else if (angle >= 157.5f && angle < 202.5f) { arrow = "←"; compass = "W"; }
        else if (angle >= 202.5f && angle < 247.5f) { arrow = "↙"; compass = "SW"; }
        else if (angle >= 247.5f && angle < 292.5f) { arrow = "↓"; compass = "S"; }
        else if (angle >= 292.5f && angle < 337.5f) { arrow = "↘"; compass = "SE"; }

        return $"Radar: {closestDist:F1}m [{compass}] {arrow}";
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}

public class ShockwaveExpander : MonoBehaviour
{
    private float elapsed = 0f;
    private float duration = 0.55f;
    private Vector3 initialScale = new Vector3(0.5f, 0.5f, 1f);
    private Vector3 maxScale = new Vector3(18f, 18f, 1f);
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        transform.localScale = initialScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        transform.localScale = Vector3.Lerp(initialScale, maxScale, Mathf.Sin(t * Mathf.PI * 0.5f));

        if (sr != null)
        {
            Color c = sr.color;
            c.a = Mathf.Lerp(0.9f, 0f, t);
            sr.color = c;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
