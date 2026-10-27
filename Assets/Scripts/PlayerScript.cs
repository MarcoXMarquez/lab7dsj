using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM || true
using UnityEngine.InputSystem;
#endif

public class PlayerScript : MonoBehaviour
{
    [Header("Tile-Locked Grid Movement (GBA Pokemon)")]
    public float Speed = 5f; // Tiles per second (walking)
    public float RunSpeed = 8f; // Tiles per second (running)

    public bool isMovingToTile = false;
    private Vector2 startTilePos;
    private Vector2 targetTilePos;
    private float moveProgress = 0f;

    public Vector2 TargetPosition => isMovingToTile ? targetTilePos : (Vector2)transform.position;

    [Header("Sprite Animations")]
    public SpriteRenderer spriteRenderer;
    public float animationFps = 8f;

    public Sprite idleDown;
    public Sprite[] walkDown;
    public Sprite[] runDown;

    public Sprite idleUp;
    public Sprite[] walkUp;
    public Sprite[] runUp;

    public Sprite idleLeft;
    public Sprite[] walkLeft;
    public Sprite[] runLeft;

    public Sprite idleRight;
    public Sprite[] walkRight;
    public Sprite[] runRight;

    public enum Direction { Down, Up, Left, Right }
    public Direction currentDirection = Direction.Down;
    private bool isRunning = false;
    private float animationTimer = 0f;
    private int frameIndex = 0;
    private Rigidbody2D rb;

    [Header("Spin Tile Mechanic (GBA Rocket Hideout)")]
    public bool isSpinning = false;
    public Vector2 spinDirection = Vector2.zero;
    public float spinSlideSpeed = 8.5f;
    private float spinVisualTimer = 0f;
    private int spinVisualIndex = 0;

    public Vector2 FacingVector
    {
        get
        {
            switch (currentDirection)
            {
                case Direction.Up: return Vector2.up;
                case Direction.Down: return Vector2.down;
                case Direction.Left: return Vector2.left;
                case Direction.Right: return Vector2.right;
                default: return Vector2.down;
            }
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // If in original Lab 07 activity scene (no GridManager in the scene)
        if (GridManager.Instance == null)
        {
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            return;
        }

        if (rb != null)
        {
            // Set Kinematic to prevent physics engine from ever pushing or shifting characters off-tile
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        LoadSpritesIfMissing();
    }

    void Start()
    {
        if (GridManager.Instance == null)
        {
            return;
        }

        // Keep the user's manual position exactly as set in the Inspector
        startTilePos = transform.position;
        targetTilePos = transform.position;
        if (rb != null) rb.position = (Vector2)transform.position;
        isMovingToTile = false;

        UpdateSprite();
    }

    void Update()
    {
        // 0. If in original Lab 07 activity scene (no GridManager in the scene)
        if (GridManager.Instance == null)
        {
            UpdateOriginalLabMovement();
            return;
        }

        // 1. Forced Slide on Arrow Pads (Spin Tiles)
        if (isSpinning)
        {
            UpdateSpinMovement();
            return;
        }

        // 2. Active Step to Target Tile
        if (isMovingToTile)
        {
            UpdateStepMovement();
            return;
        }

        // 3. Idle on Tile: Read Input to initiate next step
        Vector2 input = GetGridInput();
        if (input != Vector2.zero)
        {
            TryStartStep(input);
        }
        else
        {
            frameIndex = 0;
            animationTimer = 0f;
            UpdateSprite();
        }
    }

    [Header("Lab 07 Free Movement & Rotation (Twin-Stick / Mouse Aim)")]
    public bool useMouseAim = true;
    public float rotationSpeed = 900f;
    public bool faceVelocityRotation = false;

    public Vector2 CurrentAimDirection { get; private set; } = Vector2.right;
    public float CurrentAimAngle { get; private set; } = 0f;

    private void UpdateOriginalLabMovement()
    {
        // Toggle rotation mode with Tab (Mouse Aim <-> Movement Aim)
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            useMouseAim = !useMouseAim;
        }

        Vector2 moveDir = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            moveDir += Vector2.left;
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            moveDir += Vector2.right;
        }
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveDir += Vector2.up;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveDir += Vector2.down;
        }

        Vector2 targetVelocity = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized * Speed : Vector2.zero;

        if (rb != null)
        {
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            transform.position += (Vector3)(targetVelocity * Time.deltaTime);
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
        }

        // --- OPTION B: TWIN-STICK / MOUSE AIM ROTATION ---
        Vector2 desiredFacing = Vector2.zero;

        if (useMouseAim)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 mouseScreen = Input.mousePosition;
                mouseScreen.z = -mainCam.transform.position.z;
                Vector3 mouseWorld = mainCam.ScreenToWorldPoint(mouseScreen);
                desiredFacing = (Vector2)mouseWorld - (Vector2)transform.position;
            }
        }

        // Fallback to movement direction if mouse is right on top of player or mouse aim is disabled
        if (desiredFacing.sqrMagnitude < 0.04f)
        {
            if (moveDir.sqrMagnitude > 0.001f)
            {
                desiredFacing = moveDir;
            }
        }

        if (desiredFacing.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(desiredFacing.y, desiredFacing.x) * Mathf.Rad2Deg;
            float currentZ = transform.eulerAngles.z;
            float smoothZ = Mathf.MoveTowardsAngle(currentZ, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, smoothZ);

            CurrentAimAngle = smoothZ;
            float rad = smoothZ * Mathf.Deg2Rad;
            CurrentAimDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }

    private void UpdateStepMovement()
    {
        float currentSpeed = isRunning ? RunSpeed : Speed;
        moveProgress += currentSpeed * Time.deltaTime;

        Vector3 newPos = Vector3.Lerp(startTilePos, targetTilePos, Mathf.Clamp01(moveProgress));
        transform.position = newPos;
        if (rb != null) rb.position = (Vector2)newPos;

        // Animate
        animationTimer += Time.deltaTime;
        float stepTime = 1f / (isRunning ? (animationFps * 1.5f) : animationFps);
        if (animationTimer >= stepTime)
        {
            animationTimer = 0f;
            frameIndex = (frameIndex + 1) % 4;
        }
        UpdateSprite();

        // Step finished
        if (moveProgress >= 1f)
        {
            transform.position = (Vector3)targetTilePos;
            if (rb != null) rb.position = targetTilePos;
            startTilePos = targetTilePos;
            isMovingToTile = false;
            moveProgress = 0f;

            // Immediately check if key is held for smooth continuous tile walking
            Vector2 nextInput = GetGridInput();
            if (nextInput != Vector2.zero)
            {
                TryStartStep(nextInput);
            }
            else
            {
                frameIndex = 0;
                UpdateSprite();
            }
        }
    }

    private void TryStartStep(Vector2 dir)
    {
        // Enforce STRICT cardinal direction
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            currentDirection = dir.x > 0 ? Direction.Right : Direction.Left;
            dir = dir.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            currentDirection = dir.y > 0 ? Direction.Up : Direction.Down;
            dir = dir.y > 0 ? Vector2.up : Vector2.down;
        }

        Vector2 currentPos = transform.position;
        Vector2 nextPos = currentPos + dir * 1.0f;

        // Check if next tile is an obstacle or occupied by an enemy
        if (!IsTileBlocked(nextPos))
        {
            isMovingToTile = true;
            startTilePos = currentPos;
            targetTilePos = nextPos;
            moveProgress = 0f;
        }
        else
        {
            // Classic Pokémon behavior: face obstacle/character and play walk-in-place animation without moving!
            animationTimer += Time.deltaTime;
            float stepTime = 1f / animationFps;
            if (animationTimer >= stepTime)
            {
                animationTimer = 0f;
                frameIndex = (frameIndex + 1) % 4;
            }
            UpdateSprite();
        }
    }

    public bool IsTileBlocked(Vector2 targetPos)
    {
        // 1. Map bounds
        if (targetPos.x < -15.0f || targetPos.x > 15.0f || targetPos.y < -11.0f || targetPos.y > 11.0f)
        {
            return true;
        }

        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer == -1) envLayer = 8;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        Vector2 checkPos = targetPos + (col != null ? col.offset : Vector2.zero);
        float radius = col != null ? col.radius * 0.95f : 0.35f;

        // 2. Check wall colliders
        Collider2D hit = Physics2D.OverlapCircle(checkPos, radius, 1 << envLayer);
        if (hit != null && !hit.isTrigger) return true;

        // 3. Check if occupied by any enemy (prevents entering enemy tile or pushing enemy!)
        if (GridManager.Instance != null && GridManager.Instance.IsPositionOccupiedByAnyEnemy(targetPos))
        {
            return true;
        }

        return false;
    }

    private void UpdateSpinMovement()
    {
        Vector2 current = transform.position;
        Vector2 nextPosAhead = current + spinDirection * 0.6f;

        // If the position ahead is a wall or obstacle:
        if (IsTileBlocked(nextPosAhead))
        {
            // Stop spinning cleanly before entering the wall
            isSpinning = false;
            spinDirection = Vector2.zero;
            startTilePos = transform.position;
            targetTilePos = transform.position;
            currentDirection = Direction.Down;
            frameIndex = 0;
            UpdateSprite();
            return;
        }

        // 1. Move player strictly along spinDirection at spinSlideSpeed
        Vector3 delta = (Vector3)(spinDirection * spinSlideSpeed * Time.deltaTime);
        transform.position += delta;
        if (rb != null) rb.position = (Vector2)transform.position;

        // 2. Authentic GBA sprite spinning animation (Down -> Left -> Up -> Right)
        spinVisualTimer += Time.deltaTime;
        if (spinVisualTimer >= 0.07f)
        {
            spinVisualTimer = 0f;
            spinVisualIndex = (spinVisualIndex + 1) % 4;

            switch (spinVisualIndex)
            {
                case 0: currentDirection = Direction.Down; break;
                case 1: currentDirection = Direction.Left; break;
                case 2: currentDirection = Direction.Up; break;
                case 3: currentDirection = Direction.Right; break;
            }
        }

        frameIndex = 0;
        UpdateSprite();
    }

    public void TriggerArrowPad(Vector2 newDirection, Vector3 padCenter)
    {
        isSpinning = true;
        spinDirection = newDirection;
        isMovingToTile = false;
    }

    public void TriggerStopPad(Vector3 padCenter)
    {
        if (!isSpinning) return;

        isSpinning = false;
        spinDirection = Vector2.zero;
        isMovingToTile = false;
        startTilePos = transform.position;
        targetTilePos = transform.position;

        currentDirection = Direction.Down;
        frameIndex = 0;
        UpdateSprite();
    }

    private Vector2 GetGridInput()
    {
        Vector2 move = Vector2.zero;
        isRunning = false;

        // 1. New Input System
        try
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) move.x -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) move.x += 1f;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed) move.y += 1f;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) move.y -= 1f;

                if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
                {
                    isRunning = true;
                }
            }
        }
        catch (System.Exception) { }

        // 2. Legacy Input Manager fallback
        try
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) move.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) move.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) move.y -= 1f;

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                isRunning = true;
            }
        }
        catch (System.Exception) { }

        return move;
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        Sprite[] currentWalkCycle = null;
        Sprite[] currentRunCycle = null;
        Sprite currentIdle = null;

        switch (currentDirection)
        {
            case Direction.Down:
                currentIdle = idleDown;
                currentWalkCycle = walkDown;
                currentRunCycle = runDown;
                break;
            case Direction.Up:
                currentIdle = idleUp;
                currentWalkCycle = walkUp;
                currentRunCycle = runUp;
                break;
            case Direction.Left:
                currentIdle = idleLeft;
                currentWalkCycle = walkLeft;
                currentRunCycle = runLeft;
                break;
            case Direction.Right:
                currentIdle = idleRight;
                currentWalkCycle = walkRight;
                currentRunCycle = runRight;
                break;
        }

        bool moving = isMovingToTile || isSpinning;

        if (!moving || currentWalkCycle == null || currentWalkCycle.Length == 0)
        {
            if (currentIdle != null) spriteRenderer.sprite = currentIdle;
            return;
        }

        Sprite[] activeCycle = (isRunning && currentRunCycle != null && currentRunCycle.Length > 0)
            ? currentRunCycle
            : currentWalkCycle;

        int cycleIndex = frameIndex % 4;
        int spriteIndex = 0;
        if (cycleIndex == 0) spriteIndex = 0;
        else if (cycleIndex == 1) spriteIndex = 0;
        else if (cycleIndex == 2) spriteIndex = activeCycle.Length > 1 ? 1 : 0;
        else if (cycleIndex == 3) spriteIndex = activeCycle.Length > 1 ? 1 : 0;

        if (spriteIndex < activeCycle.Length && activeCycle[spriteIndex] != null)
        {
            spriteRenderer.sprite = activeCycle[spriteIndex];
        }
        else if (currentIdle != null)
        {
            spriteRenderer.sprite = currentIdle;
        }
    }

    private void LoadSpritesIfMissing()
    {
        if (idleDown == null) idleDown = Resources.Load<Sprite>("Red/red_down_idle");
        if (idleUp == null) idleUp = Resources.Load<Sprite>("Red/red_up_idle");
        if (idleLeft == null) idleLeft = Resources.Load<Sprite>("Red/red_left_idle");
        if (idleRight == null) idleRight = Resources.Load<Sprite>("Red/red_right_idle");

        if (walkDown == null || walkDown.Length == 0)
        {
            walkDown = new Sprite[2];
            walkDown[0] = Resources.Load<Sprite>("Red/red_down_walk_0");
            walkDown[1] = Resources.Load<Sprite>("Red/red_down_walk_1");
        }
        if (walkUp == null || walkUp.Length == 0)
        {
            walkUp = new Sprite[2];
            walkUp[0] = Resources.Load<Sprite>("Red/red_up_walk_0");
            walkUp[1] = Resources.Load<Sprite>("Red/red_up_walk_1");
        }
        if (walkLeft == null || walkLeft.Length == 0)
        {
            walkLeft = new Sprite[2];
            walkLeft[0] = Resources.Load<Sprite>("Red/red_left_walk_0");
            walkLeft[1] = Resources.Load<Sprite>("Red/red_left_walk_1");
        }
        if (walkRight == null || walkRight.Length == 0)
        {
            walkRight = new Sprite[2];
            walkRight[0] = Resources.Load<Sprite>("Red/red_right_walk_0");
            walkRight[1] = Resources.Load<Sprite>("Red/red_right_walk_1");
        }

        if (runDown == null || runDown.Length == 0)
        {
            runDown = new Sprite[2];
            runDown[0] = Resources.Load<Sprite>("Red/red_down_run_0");
            runDown[1] = Resources.Load<Sprite>("Red/red_down_run_1");
        }
        if (runUp == null || runUp.Length == 0)
        {
            runUp = new Sprite[2];
            runUp[0] = Resources.Load<Sprite>("Red/red_up_run_0");
            runUp[1] = Resources.Load<Sprite>("Red/red_up_run_1");
        }
        if (runLeft == null || runLeft.Length == 0)
        {
            runLeft = new Sprite[2];
            runLeft[0] = Resources.Load<Sprite>("Red/red_left_run_0");
            runLeft[1] = Resources.Load<Sprite>("Red/red_left_run_1");
        }
        if (runRight == null || runRight.Length == 0)
        {
            runRight = new Sprite[2];
            runRight[0] = Resources.Load<Sprite>("Red/red_right_run_0");
            runRight[1] = Resources.Load<Sprite>("Red/red_right_run_1");
        }
    }
}
