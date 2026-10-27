using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

/// <summary>
/// Lab07TacticalHUD: High-tech military infiltration HUD inspired by Alien Swarm,
/// Metal Gear Solid (Soliton Radar), and Cyberpunk 2077.
/// Provides genuine tactical assistance: Soliton motion radar with sonar sweep,
/// segmented ballistic health with ghost bar, directional objective nav-beacon,
/// edge-of-screen threat chevrons, EMP capacitor gauge, and mouse aim reticle.
/// </summary>
public class Lab07TacticalHUD : MonoBehaviour
{
    public static Lab07TacticalHUD Instance { get; private set; }

    [Header("Color Palette (High-Tech Hologram)")]
    public Color cyanPrimary = new Color(0f, 0.94f, 1f, 0.95f);       // #00F0FF
    public Color cyanMuted = new Color(0f, 0.94f, 1f, 0.35f);
    public Color amberAccent = new Color(1f, 0.72f, 0.05f, 0.95f);     // #FFB800
    public Color threatRed = new Color(1f, 0.2f, 0.3f, 0.95f);         // #FF334D
    public Color darkBackground = new Color(0.03f, 0.06f, 0.1f, 0.88f);// Deep Carbon Slate
    public Color panelBorder = new Color(0.1f, 0.45f, 0.65f, 0.75f);

    [Header("Radar Settings")]
    public float radarRadius = 75f;        // 150px diameter
    public float radarWorldRange = 16f;     // Meters represented on radar
    public float sweepSpeed = 2.0f;        // Revolutions per second
    private float sweepAngle = 0f;

    [Header("Ghost Health Bar Animation")]
    private float ghostHealth = 3f;
    private float ghostTimer = 0f;

    [Header("Cached References")]
    private PlayerScript player;
    private Lab07GameManager gameManager;
    private AIPath[] cachedEnemies;
    private CollectibleItem[] cachedItems;
    private DynamicBlastDoor[] cachedDoors;
    private AstarPathVisualizer pathVisualizer;

    // Procedural UI Textures
    private Texture2D texWhite;
    private Texture2D texPanelBg;
    private Texture2D texCircle;
    private Texture2D texDot;
    private Texture2D texRadarGrid;
    private Texture2D texCrosshair;

    // Custom GUI Styles
    private GUIStyle headerStyle;
    private GUIStyle subHeaderStyle;
    private GUIStyle monoStyle;
    private GUIStyle boldMonoStyle;
    private GUIStyle alertStyle;
    private bool stylesInitialized = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
            return;
        }

        GenerateTextures();
    }

    void Start()
    {
        RefreshReferences();
    }

    public void RefreshReferences()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) player = pObj.GetComponent<PlayerScript>();

        gameManager = Lab07GameManager.Instance;
        cachedEnemies = FindObjectsByType<AIPath>(FindObjectsSortMode.None);
        cachedItems = FindObjectsByType<CollectibleItem>(FindObjectsSortMode.None);
        cachedDoors = FindObjectsByType<DynamicBlastDoor>(FindObjectsSortMode.None);
        pathVisualizer = FindFirstObjectByType<AstarPathVisualizer>();

        if (gameManager != null)
        {
            ghostHealth = gameManager.playerLives;
        }
    }

    void Update()
    {
        // Advance radar sonar sweep
        sweepAngle = (sweepAngle + sweepSpeed * 360f * Time.deltaTime) % 360f;

        // Smooth ghost health decay
        if (gameManager != null)
        {
            if (ghostHealth > gameManager.playerLives)
            {
                ghostTimer += Time.deltaTime;
                if (ghostTimer > 0.4f)
                {
                    ghostHealth = Mathf.MoveTowards(ghostHealth, gameManager.playerLives, Time.deltaTime * 2.5f);
                }
            }
            else
            {
                ghostHealth = gameManager.playerLives;
                ghostTimer = 0f;
            }
        }
    }

    private void GenerateTextures()
    {
        // 1. Single white pixel for flat tinted boxes
        texWhite = new Texture2D(1, 1);
        texWhite.SetPixel(0, 0, Color.white);
        texWhite.Apply();

        // 2. Dark panel with subtle scanline effect
        texPanelBg = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float scanline = (y % 4 == 0) ? 0.85f : 1.0f;
                Color col = darkBackground * scanline;
                col.a = darkBackground.a;
                texPanelBg.SetPixel(x, y, col);
            }
        }
        texPanelBg.Apply();

        // 3. Smooth antialiased circle for radar
        int cSize = 128;
        texCircle = new Texture2D(cSize, cSize);
        float radius = cSize * 0.5f - 1.5f;
        Vector2 center = new Vector2(cSize * 0.5f, cSize * 0.5f);
        for (int y = 0; y < cSize; y++)
        {
            for (int x = 0; x < cSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float borderDist = Mathf.Abs(dist - radius);
                if (dist > radius + 1f)
                {
                    texCircle.SetPixel(x, y, Color.clear);
                }
                else if (borderDist < 2.0f)
                {
                    float alpha = Mathf.Clamp01(2.0f - borderDist);
                    texCircle.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    texCircle.SetPixel(x, y, new Color(0.04f, 0.08f, 0.12f, 0.85f));
                }
            }
        }
        texCircle.Apply();

        // 4. Smooth antialiased dot for radar blips
        int dSize = 32;
        texDot = new Texture2D(dSize, dSize);
        float dRad = dSize * 0.5f - 1f;
        Vector2 dCenter = new Vector2(dSize * 0.5f, dSize * 0.5f);
        for (int y = 0; y < dSize; y++)
        {
            for (int x = 0; x < dSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), dCenter);
                float alpha = Mathf.Clamp01((dRad - dist) * 1.5f);
                texDot.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texDot.Apply();

        // 5. Tactical Crosshair Reticle
        int retSize = 48;
        texCrosshair = new Texture2D(retSize, retSize);
        Vector2 retCenter = new Vector2(retSize * 0.5f, retSize * 0.5f);
        for (int y = 0; y < retSize; y++)
        {
            for (int x = 0; x < retSize; x++)
            {
                texCrosshair.SetPixel(x, y, Color.clear);
                float d = Vector2.Distance(new Vector2(x, y), retCenter);
                // Center dot
                if (d < 2.5f) texCrosshair.SetPixel(x, y, cyanPrimary);
                // Inner ring
                else if (Mathf.Abs(d - 12f) < 1.2f) texCrosshair.SetPixel(x, y, new Color(0f, 0.94f, 1f, 0.7f));
                // 4 Crosshair ticks
                else if ((Mathf.Abs(x - retCenter.x) <= 1 && Mathf.Abs(y - retCenter.y) >= 14 && Mathf.Abs(y - retCenter.y) <= 22) ||
                         (Mathf.Abs(y - retCenter.y) <= 1 && Mathf.Abs(x - retCenter.x) >= 14 && Mathf.Abs(x - retCenter.x) <= 22))
                {
                    texCrosshair.SetPixel(x, y, cyanPrimary);
                }
            }
        }
        texCrosshair.Apply();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = cyanPrimary;

        subHeaderStyle = new GUIStyle(GUI.skin.label);
        subHeaderStyle.fontSize = 11;
        subHeaderStyle.fontStyle = FontStyle.Bold;
        subHeaderStyle.normal.textColor = new Color(0.7f, 0.85f, 0.95f);

        monoStyle = new GUIStyle(GUI.skin.label);
        monoStyle.fontSize = 12;
        monoStyle.normal.textColor = new Color(0.85f, 0.92f, 1f);

        boldMonoStyle = new GUIStyle(GUI.skin.label);
        boldMonoStyle.fontSize = 12;
        boldMonoStyle.fontStyle = FontStyle.Bold;
        boldMonoStyle.normal.textColor = Color.white;

        alertStyle = new GUIStyle(GUI.skin.label);
        alertStyle.fontSize = 14;
        alertStyle.fontStyle = FontStyle.Bold;
        alertStyle.alignment = TextAnchor.MiddleCenter;
        alertStyle.normal.textColor = threatRed;

        stylesInitialized = true;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) return;
        InitStyles();

        // 1. Top-Left: Operative Vitals & Biometrics
        DrawVitalsPanel(20, 20);

        // 2. Top-Center: Objective Tracker & Directional Compass
        DrawObjectiveBeacon(Screen.width * 0.5f - 180, 20);

        // 3. Top-Right: Soliton Motion Radar
        DrawSolitonRadar(Screen.width - (radarRadius * 2f + 30f), 20);

        // 4. Bottom-Right: Tactical EMP Ordnance & Controls Guide
        DrawEMPAndControlsPanel(Screen.width - 290, Screen.height - 150);

        // 5. In-World Threat Warning Chevrons (Edge of screen alerts)
        DrawThreatChevrons();

        // 6. Diegetic Interaction Prompt (Near Door)
        DrawInteractionPrompt();

        // 7. Academic Telemetry Deck (A* Inspector)
        if (gameManager != null && gameManager.showDebugHUD)
        {
            DrawAStarTelemetry(20, Screen.height - 145);
        }

        // 8. Mouse Crosshair (When Mouse Aim is active)
        if (player != null && player.useMouseAim)
        {
            DrawMouseCrosshair();
        }
    }

    // ==========================================
    // 1. TOP-LEFT: BIOMETRICS & BALLISTIC SHIELD
    // ==========================================
    private void DrawVitalsPanel(float x, float y)
    {
        float w = 290;
        float h = 105;
        DrawTechFrame(x, y, w, h, "OPERATIVE // AGENT-07", cyanPrimary);

        int lives = gameManager != null ? gameManager.playerLives : 3;
        int maxL = gameManager != null ? gameManager.maxLives : 3;

        // Status pill
        GUI.color = lives > 1 ? new Color(0.2f, 1f, 0.4f) : threatRed;
        GUI.Label(new Rect(x + w - 85, y + 6, 75, 18), lives > 1 ? "[ONLINE]" : "[CRITICAL]", subHeaderStyle);
        GUI.color = Color.white;

        // Ballistic Armor / Health Segmented Bar
        GUI.Label(new Rect(x + 15, y + 32, 100, 18), "BALLISTIC SHIELD", subHeaderStyle);

        float barStartX = x + 120;
        float segmentW = 46;
        float segmentH = 14;
        float segmentGap = 6;

        for (int i = 0; i < maxL; i++)
        {
            Rect segRect = new Rect(barStartX + i * (segmentW + segmentGap), y + 34, segmentW, segmentH);

            // Background cell
            DrawSolidRect(segRect, new Color(0.12f, 0.18f, 0.25f, 0.8f));

            // Ghost damage fill
            if (ghostHealth > i)
            {
                float ghostFill = Mathf.Clamp01(ghostHealth - i);
                Rect ghostRect = new Rect(segRect.x, segRect.y, segRect.width * ghostFill, segRect.height);
                DrawSolidRect(ghostRect, new Color(1f, 0.8f, 0.4f, 0.5f));
            }

            // Current Health fill
            if (lives > i)
            {
                Color cellCol = lives switch
                {
                    3 => cyanPrimary,
                    2 => amberAccent,
                    _ => threatRed
                };
                DrawSolidRect(segRect, cellCol);
                // Top highlight line
                DrawSolidRect(new Rect(segRect.x, segRect.y, segRect.width, 2), Color.white * 0.7f);
            }

            // Segment Border
            DrawWireRect(segRect, 1, panelBorder);
        }

        // Sprint Overclock / Speed Boost Bar
        float boostRemaining = gameManager != null ? gameManager.speedBoostRemaining : 0f;
        GUI.Label(new Rect(x + 15, y + 58, 100, 18), "OVERCLOCK", subHeaderStyle);

        Rect boostRect = new Rect(barStartX, y + 60, (segmentW * 3) + (segmentGap * 2), 8);
        DrawSolidRect(boostRect, new Color(0.1f, 0.15f, 0.2f, 0.8f));

        if (boostRemaining > 0f)
        {
            float fillRatio = Mathf.Clamp01(boostRemaining / 5.0f);
            Rect activeFill = new Rect(boostRect.x, boostRect.y, boostRect.width * fillRatio, boostRect.height);
            DrawSolidRect(activeFill, amberAccent);
            GUI.color = amberAccent;
            GUI.Label(new Rect(x + 15, y + 78, 250, 18), $"TURBO CHARGE ACTIVE [{boostRemaining:F1}s]", monoStyle);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = new Color(0.6f, 0.7f, 0.8f);
            GUI.Label(new Rect(x + 15, y + 78, 250, 18), "READY // MOVE: [WASD]  AIM: [MOUSE]", monoStyle);
            GUI.color = Color.white;
        }
    }

    // ==========================================
    // 2. TOP-CENTER: MISSION OBJECTIVE & BEACON
    // ==========================================
    private void DrawObjectiveBeacon(float x, float y)
    {
        float w = 360;
        float h = 68;
        DrawTechFrame(x, y, w, h, "DIRECTIVE // INTEL EXTRACTION", amberAccent);

        int collected = gameManager != null ? gameManager.collectedIntel : 0;
        int total = gameManager != null ? gameManager.totalIntel : 8;

        // Progress text
        GUI.Label(new Rect(x + 15, y + 26, 200, 18), $"INTEL DISKS: {collected} / {total} SECURED", boldMonoStyle);

        // Segmented Progress Bar
        float progBarW = 120;
        float progBarH = 10;
        float progBarX = x + w - progBarW - 15;
        Rect progRect = new Rect(progBarX, y + 29, progBarW, progBarH);
        DrawSolidRect(progRect, new Color(0.1f, 0.15f, 0.2f, 0.85f));

        float pct = total > 0 ? (float)collected / total : 0f;
        DrawSolidRect(new Rect(progRect.x, progRect.y, progRect.width * pct, progRect.height), cyanPrimary);
        DrawWireRect(progRect, 1, panelBorder);

        // Directional Beacon to closest uncollected Intel
        string beaconInfo = GetDirectionalBeaconInfo(out bool isNear);
        GUIStyle bStyle = isNear ? alertStyle : monoStyle;
        if (isNear)
        {
            float blink = Mathf.PingPong(Time.time * 4f, 1f);
            GUI.color = Color.Lerp(cyanPrimary, amberAccent, blink);
        }
        else
        {
            GUI.color = new Color(0.3f, 0.95f, 1f);
        }

        GUI.Label(new Rect(x + 15, y + 45, w - 30, 18), beaconInfo, bStyle);
        GUI.color = Color.white;
    }

    private string GetDirectionalBeaconInfo(out bool isNear)
    {
        isNear = false;
        if (player == null) return "TARGET: SCANNING...";

        Vector2 pPos = player.transform.position;
        float minDist = float.MaxValue;
        Vector2 targetPos = Vector2.zero;
        bool found = false;

        if (cachedItems != null)
        {
            foreach (var item in cachedItems)
            {
                if (item != null && item.gameObject.activeInHierarchy && item.itemType == CollectibleItem.ItemType.IntelDisk)
                {
                    float d = Vector2.Distance(pPos, item.transform.position);
                    if (d < minDist)
                    {
                        minDist = d;
                        targetPos = item.transform.position;
                        found = true;
                    }
                }
            }
        }

        if (!found) return "✔ ALL INTEL SECURED - HEAD TO EXTRACTION";

        if (minDist < 3.5f)
        {
            isNear = true;
            return $"⚡ INTEL IN PROXIMITY ({minDist:F1}m) - ACQUIRE DISK!";
        }

        // Calculate 8-direction cardinal arrow
        Vector2 diff = targetPos - pPos;
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        string arrow = angle switch
        {
            >= 337.5f or < 22.5f => "▶ EAST",
            >= 22.5f and < 67.5f => "↗ NORTH-EAST",
            >= 67.5f and < 112.5f => "▲ NORTH",
            >= 112.5f and < 157.5f => "↖ NORTH-WEST",
            >= 157.5f and < 202.5f => "◀ WEST",
            >= 202.5f and < 247.5f => "↙ SOUTH-WEST",
            >= 247.5f and < 292.5f => "▼ SOUTH",
            _ => "↘ SOUTH-EAST"
        };

        return $"BEACON: {minDist:F1}m [{arrow}]";
    }

    // ==========================================
    // 3. TOP-RIGHT: SOLITON MOTION RADAR
    // ==========================================
    private void DrawSolitonRadar(float x, float y)
    {
        float size = radarRadius * 2f;
        Rect radarRect = new Rect(x, y, size, size);

        // 1. Radar background circle
        GUI.color = Color.white;
        GUI.DrawTexture(radarRect, texCircle);

        Vector2 center = new Vector2(x + radarRadius, y + radarRadius);

        // 2. Range rings
        float innerR = radarRadius * (6f / radarWorldRange);
        float midR = radarRadius * (11f / radarWorldRange);
        DrawWireCircle(center, innerR, new Color(0f, 0.94f, 1f, 0.25f));
        DrawWireCircle(center, midR, new Color(0f, 0.94f, 1f, 0.35f));

        // 3. Radar Crosshairs
        DrawSolidRect(new Rect(center.x - radarRadius, center.y, size, 1), new Color(0f, 0.94f, 1f, 0.2f));
        DrawSolidRect(new Rect(center.x, center.y - radarRadius, 1, size), new Color(0f, 0.94f, 1f, 0.2f));

        // 4. Rotating Sonar Sweep Line
        float rad = sweepAngle * Mathf.Deg2Rad;
        Vector2 sweepEnd = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * (radarRadius - 4f);
        DrawAALine(center, sweepEnd, cyanPrimary, 1.8f);

        // 5. Radar Cardinal Labels
        GUI.color = cyanMuted;
        GUI.Label(new Rect(center.x - 5, y + 2, 12, 14), "N", subHeaderStyle);
        GUI.Label(new Rect(x + size - 14, center.y - 7, 12, 14), "E", subHeaderStyle);
        GUI.Label(new Rect(center.x - 5, y + size - 16, 12, 14), "S", subHeaderStyle);
        GUI.Label(new Rect(x + 4, center.y - 7, 12, 14), "W", subHeaderStyle);
        GUI.color = Color.white;

        if (player == null) return;
        Vector2 playerPos = player.transform.position;

        // 6. Draw Intel Disks (Cyan/Gold Blips)
        if (cachedItems != null)
        {
            foreach (var item in cachedItems)
            {
                if (item != null && item.gameObject.activeInHierarchy && item.itemType == CollectibleItem.ItemType.IntelDisk)
                {
                    Vector2 rel = (Vector2)item.transform.position - playerPos;
                    if (rel.magnitude <= radarWorldRange)
                    {
                        Vector2 blipPos = center + new Vector2(rel.x, -rel.y) * (radarRadius / radarWorldRange);
                        DrawBlip(blipPos, 6f, amberAccent);
                    }
                }
            }
        }

        // 7. Draw Enemy Hunters (Red Threat Blips with Pulse)
        if (cachedEnemies != null)
        {
            foreach (var enemy in cachedEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    Vector2 rel = (Vector2)enemy.transform.position - playerPos;
                    float dist = rel.magnitude;
                    if (dist <= radarWorldRange)
                    {
                        Vector2 blipPos = center + new Vector2(rel.x, -rel.y) * (radarRadius / radarWorldRange);
                        float pulse = dist < 8f ? (Mathf.Sin(Time.time * 8f) * 2f + 8f) : 7f;
                        DrawBlip(blipPos, pulse, threatRed);
                    }
                }
            }
        }

        // 8. Player Center Dot and Aim Direction Cone
        DrawBlip(center, 7f, cyanPrimary);
        float aimAngle = player.CurrentAimAngle;
        float aimRad = aimAngle * Mathf.Deg2Rad;
        Vector2 aimDir = new Vector2(Mathf.Cos(aimRad), -Mathf.Sin(aimRad)); // Invert Y for GUI space
        DrawAALine(center, center + aimDir * 18f, cyanPrimary, 2.5f);

        // Header caption below radar
        GUI.color = cyanMuted;
        GUI.Label(new Rect(x, y + size + 4, size, 16), "SOLITON SENSOR // 16M", subHeaderStyle);
        GUI.color = Color.white;
    }

    private void DrawBlip(Vector2 screenPos, float size, Color col)
    {
        GUI.color = col;
        GUI.DrawTexture(new Rect(screenPos.x - size * 0.5f, screenPos.y - size * 0.5f, size, size), texDot);
        GUI.color = Color.white;
    }

    // ==========================================
    // 4. BOTTOM-RIGHT: EMP ORDNANCE & CONTROLS
    // ==========================================
    private void DrawEMPAndControlsPanel(float x, float y)
    {
        float w = 270;
        float h = 130;
        DrawTechFrame(x, y, w, h, "TACTICAL ORDNANCE", cyanPrimary);

        int charges = gameManager != null ? gameManager.empCharges : 0;
        float stunLeft = gameManager != null ? gameManager.empStunRemaining : 0f;

        // EMP Power Battery Cells
        GUI.Label(new Rect(x + 15, y + 26, 120, 18), "EMP SHOCKWAVE", subHeaderStyle);

        float cellW = 38;
        float cellH = 14;
        for (int i = 0; i < 3; i++)
        {
            Rect cellRect = new Rect(x + 130 + i * (cellW + 6), y + 28, cellW, cellH);
            DrawSolidRect(cellRect, new Color(0.1f, 0.15f, 0.2f, 0.8f));

            if (charges > i)
            {
                DrawSolidRect(cellRect, cyanPrimary);
                DrawSolidRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 2), Color.white);
            }
            DrawWireRect(cellRect, 1, panelBorder);
        }

        if (stunLeft > 0f)
        {
            GUI.color = amberAccent;
            GUI.Label(new Rect(x + 15, y + 50, w - 30, 18), $"⚡ JAMMER ACTIVE: {stunLeft:F1}s REMAINING", boldMonoStyle);
            GUI.color = Color.white;
        }
        else if (charges > 0)
        {
            GUI.color = cyanPrimary;
            GUI.Label(new Rect(x + 15, y + 50, w - 30, 18), "⚡ [SPACE] DETONATE EMP PULSE", boldMonoStyle);
            GUI.color = Color.white;
        }
        else
        {
            GUI.color = Color.gray;
            GUI.Label(new Rect(x + 15, y + 50, w - 30, 18), "⚡ CHARGES DEPLETED", monoStyle);
            GUI.color = Color.white;
        }

        // Minimalist Keybinds
        GUI.color = new Color(0.7f, 0.85f, 0.95f);
        GUI.Label(new Rect(x + 15, y + 72, w - 30, 16), "CONTROLS // TACTICAL GUIDE", subHeaderStyle);
        GUI.Label(new Rect(x + 15, y + 90, w - 30, 16), "[TAB] Toggle Mouse / WASD Aim", monoStyle);
        GUI.Label(new Rect(x + 15, y + 106, w - 30, 16), "[Z] Sat-Cam  [E] Door  [H] Telemetry", monoStyle);
        GUI.color = Color.white;
    }

    // ==========================================
    // 5. IN-WORLD THREAT WARNING CHEVRONS
    // ==========================================
    private void DrawThreatChevrons()
    {
        if (player == null || cachedEnemies == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 pPos = player.transform.position;

        foreach (var enemy in cachedEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || !enemy.canMove) continue;

            float dist = Vector2.Distance(pPos, enemy.transform.position);
            // Alert on enemies within 10 meters that are actively chasing
            if (dist < 10f)
            {
                Vector3 viewPos = cam.WorldToViewportPoint(enemy.transform.position);
                // If enemy is off-screen
                if (viewPos.x < 0.05f || viewPos.x > 0.95f || viewPos.y < 0.05f || viewPos.y > 0.95f)
                {
                    // Clamp to screen borders
                    float edgeX = Mathf.Clamp(viewPos.x, 0.04f, 0.96f) * Screen.width;
                    float edgeY = (1f - Mathf.Clamp(viewPos.y, 0.04f, 0.96f)) * Screen.height;

                    float blink = Mathf.PingPong(Time.time * 6f, 1f);
                    Color warnCol = Color.Lerp(threatRed, Color.white, blink * 0.5f);

                    DrawSolidRect(new Rect(edgeX - 10, edgeY - 10, 20, 20), warnCol);
                    GUI.color = Color.black;
                    GUI.Label(new Rect(edgeX - 8, edgeY - 9, 16, 16), "!", boldMonoStyle);
                    GUI.color = Color.white;
                }
            }
        }
    }

    // ==========================================
    // 6. DIEGETIC INTERACTION PROMPT
    // ==========================================
    private void DrawInteractionPrompt()
    {
        if (cachedDoors == null) return;
        bool nearDoor = false;
        foreach (var d in cachedDoors)
        {
            if (d != null && d.IsPlayerNear())
            {
                nearDoor = true;
                break;
            }
        }

        if (nearDoor)
        {
            float w = 260;
            float h = 38;
            float x = Screen.width * 0.5f - w * 0.5f;
            float y = Screen.height * 0.72f;

            DrawSolidRect(new Rect(x, y, w, h), new Color(0.05f, 0.1f, 0.15f, 0.92f));
            DrawWireRect(new Rect(x, y, w, h), 2, amberAccent);

            // Bracket decorations
            DrawSolidRect(new Rect(x - 4, y, 3, h), amberAccent);
            DrawSolidRect(new Rect(x + w + 1, y, 3, h), amberAccent);

            GUI.color = amberAccent;
            GUI.Label(new Rect(x, y + 10, w, 20), "▶ [E] OVERRIDE BLAST COMPUERTA ◀", alertStyle);
            GUI.color = Color.white;
        }
    }

    // ==========================================
    // 7. ACADEMIC TELEMETRY DECK (A* INSPECTOR)
    // ==========================================
    private void DrawAStarTelemetry(float x, float y)
    {
        float w = 340;
        float h = 125;
        DrawTechFrame(x, y, w, h, "A* NEURAL ENGINE // TELEMETRY", amberAccent);

        if (pathVisualizer != null)
        {
            GUI.Label(new Rect(x + 15, y + 26, w - 30, 18), "EVALUATION FUNCTION: f(n) = g(n) + h(n)", boldMonoStyle);
            GUI.Label(new Rect(x + 15, y + 46, w - 30, 18), $"g(n) Path Distance: {pathVisualizer.calculatedPathDistance:F2} m", monoStyle);
            GUI.Label(new Rect(x + 15, y + 64, w - 30, 18), $"h(n) Direct Heuristic: {pathVisualizer.directEuclideanDistance:F2} m", monoStyle);
            GUI.Label(new Rect(x + 15, y + 82, w - 30, 18), $"f(n) Total Score: {pathVisualizer.evaluationFunctionF:F2}  |  Waypoints: {pathVisualizer.waypointCount}", monoStyle);
            GUI.color = cyanMuted;
            GUI.Label(new Rect(x + 15, y + 102, w - 30, 16), "[H] Toggle Academic Inspector", subHeaderStyle);
            GUI.color = Color.white;
        }
    }

    // ==========================================
    // 8. TACTICAL MOUSE CROSSHAIR
    // ==========================================
    private void DrawMouseCrosshair()
    {
        Vector2 mousePos = Input.mousePosition;
        float x = mousePos.x;
        float y = Screen.height - mousePos.y; // Invert for GUI space

        float size = 32;
        GUI.color = cyanPrimary;
        GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), texCrosshair);
        GUI.color = Color.white;
    }

    // ==========================================
    // PROCEDURAL DRAWING UTILITIES
    // ==========================================
    private void DrawTechFrame(float x, float y, float w, float h, string title, Color accent)
    {
        Rect r = new Rect(x, y, w, h);
        // Background
        DrawSolidRect(r, darkBackground);

        // Thin border
        DrawWireRect(r, 1, panelBorder);

        // Top accent line
        DrawSolidRect(new Rect(x, y, w, 2), accent);

        // Corner Chamfer Brackets (4 corners)
        float bracketSize = 6f;
        DrawSolidRect(new Rect(x, y, bracketSize, 2), accent);
        DrawSolidRect(new Rect(x, y, 2, bracketSize), accent);

        DrawSolidRect(new Rect(x + w - bracketSize, y, bracketSize, 2), accent);
        DrawSolidRect(new Rect(x + w - 2, y, 2, bracketSize), accent);

        DrawSolidRect(new Rect(x, y + h - 2, bracketSize, 2), accent);
        DrawSolidRect(new Rect(x, y + h - bracketSize, 2, bracketSize), accent);

        DrawSolidRect(new Rect(x + w - bracketSize, y + h - 2, bracketSize, 2), accent);
        DrawSolidRect(new Rect(x + w - 2, y + h - bracketSize, 2, bracketSize), accent);

        // Title Header
        GUI.color = accent;
        GUI.Label(new Rect(x + 15, y + 5, w - 30, 20), title, headerStyle);
        GUI.color = Color.white;
    }

    private void DrawSolidRect(Rect r, Color c)
    {
        GUI.color = c;
        GUI.DrawTexture(r, texWhite);
        GUI.color = Color.white;
    }

    private void DrawWireRect(Rect r, float thickness, Color c)
    {
        DrawSolidRect(new Rect(r.x, r.y, r.width, thickness), c);
        DrawSolidRect(new Rect(r.x, r.y + r.height - thickness, r.width, thickness), c);
        DrawSolidRect(new Rect(r.x, r.y, thickness, r.height), c);
        DrawSolidRect(new Rect(r.x + r.width - thickness, r.y, thickness, r.height), c);
    }

    private void DrawWireCircle(Vector2 center, float radius, Color c)
    {
        float circumference = 2f * Mathf.PI * radius;
        int segments = Mathf.Clamp((int)(circumference * 0.25f), 16, 48);
        float step = (2f * Mathf.PI) / segments;
        Vector2 prev = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float a = i * step;
            Vector2 next = center + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius);
            DrawAALine(prev, next, c, 1.2f);
            prev = next;
        }
    }

    private void DrawAALine(Vector2 p1, Vector2 p2, Color c, float width)
    {
        Vector2 d = p2 - p1;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float length = d.magnitude;

        GUIUtility.RotateAroundPivot(angle, p1);
        DrawSolidRect(new Rect(p1.x, p1.y - width * 0.5f, length, width), c);
        GUIUtility.RotateAroundPivot(-angle, p1);
    }
}
