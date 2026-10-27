using System;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public enum EnemyType
    {
        AquaMale,
        AquaFemale,
        MagmaMale,
        MagmaFemale
    }

    public enum FacingDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    [Header("Original Lab 07 (AIPath)")]
    public Pathfinding.AIPath aiPath;
    private Vector2 labDirection;

    [Header("Enemy Identity")]
    public EnemyType enemyType = EnemyType.AquaMale;
    public SpriteRenderer spriteRenderer;
    public float animationFps = 8f;

    [Header("Tile-by-Tile Movement")]
    public float speed = 2.8f; // Tiles per second
    public bool isMovingToTile = false;
    private Vector2 startTilePos;
    private Vector2 targetTilePos;
    private float moveProgress = 0f;

    public Vector2 TargetPosition => isMovingToTile ? targetTilePos : (Vector2)transform.position;

    public Action OnStepFinished;

    [Header("Sprites")]
    public Sprite idleDown;
    public Sprite[] walkDown;
    public Sprite idleUp;
    public Sprite[] walkUp;
    public Sprite idleLeft;
    public Sprite[] walkLeft;
    public Sprite idleRight;
    public Sprite[] walkRight;

    public FacingDirection currentDirection = FacingDirection.Down;
    private float animationTimer = 0f;
    private int frameIndex = 0;
    private EnemyType loadedType;

    void Awake()
    {
        if (GridManager.Instance == null)
        {
            if (aiPath == null)
            {
                aiPath = GetComponent<Pathfinding.AIPath>();
                if (aiPath == null)
                {
                    aiPath = GetComponentInParent<Pathfinding.AIPath>();
                }
            }
            return;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        LoadEnemySprites(enemyType);
    }

    void Start()
    {
        if (GridManager.Instance == null)
        {
            if (aiPath == null)
            {
                aiPath = GetComponent<Pathfinding.AIPath>();
                if (aiPath == null)
                {
                    aiPath = GetComponentInParent<Pathfinding.AIPath>();
                }
            }
            return;
        }

        // Keep the user's manual position exactly as set in the Inspector
        startTilePos = transform.position;
        targetTilePos = transform.position;
        isMovingToTile = false;

        transform.localRotation = Quaternion.identity;

        if (GridManager.Instance != null)
        {
            GridManager.Instance.RegisterEnemy(this);
        }

        UpdateSprite();
    }

    void OnDestroy()
    {
        if (GridManager.Instance != null)
        {
            GridManager.Instance.UnregisterEnemy(this);
        }
    }

    void Update()
    {
        if (GridManager.Instance == null)
        {
            faceVelocity();
            return;
        }

        if (enemyType != loadedType)
        {
            LoadEnemySprites(enemyType);
        }

        if (isMovingToTile)
        {
            UpdateStepMovement();
        }
        else
        {
            frameIndex = 0;
            animationTimer = 0f;
            UpdateSprite();
        }

        // Keep sprite upright
        transform.localRotation = Quaternion.identity;
    }

    public void faceVelocity()
    {
        if (aiPath == null)
        {
            aiPath = GetComponent<Pathfinding.AIPath>();
            if (aiPath == null) aiPath = GetComponentInParent<Pathfinding.AIPath>();
        }

        if (aiPath == null) return;

        Vector2 direction = aiPath.desiredVelocity;
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.right = direction;
        }
    }

    private void UpdateStepMovement()
    {
        moveProgress += speed * Time.deltaTime;

        Vector2 newPos = Vector2.Lerp(startTilePos, targetTilePos, Mathf.Clamp01(moveProgress));
        transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);

        // Update walk animation
        animationTimer += Time.deltaTime;
        float stepTime = 1f / animationFps;
        if (animationTimer >= stepTime)
        {
            animationTimer = 0f;
            frameIndex = (frameIndex + 1) % 4;
        }
        UpdateSprite();

        if (moveProgress >= 1f)
        {
            transform.position = new Vector3(targetTilePos.x, targetTilePos.y, transform.position.z);
            startTilePos = targetTilePos;
            isMovingToTile = false;
            moveProgress = 0f;
            frameIndex = 0;
            UpdateSprite();

            OnStepFinished?.Invoke();
        }
    }

    public bool TryStepInDirection(Vector2 dir)
    {
        if (isMovingToTile) return false;

        Vector2 current = transform.position;
        Vector2 nextPos = current + dir * 1.0f;

        // Check if destination is blocked by walls, player, or other enemies
        if (IsPositionBlocked(nextPos))
        {
            return false;
        }

        // Determine cardinal direction
        if (dir.x > 0.5f)
        {
            currentDirection = FacingDirection.Right;
        }
        else if (dir.x < -0.5f)
        {
            currentDirection = FacingDirection.Left;
        }
        else if (dir.y > 0.5f)
        {
            currentDirection = FacingDirection.Up;
        }
        else
        {
            currentDirection = FacingDirection.Down;
        }

        startTilePos = current;
        targetTilePos = nextPos;
        moveProgress = 0f;
        isMovingToTile = true;

        return true;
    }

    public bool IsPositionBlocked(Vector2 targetPos)
    {
        int envLayer = LayerMask.NameToLayer("Environment");
        if (envLayer == -1) envLayer = 8;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        Vector2 checkPos = targetPos + (col != null ? col.offset : Vector2.zero);
        float radius = col != null ? col.radius * 0.95f : 0.35f;

        // 1. Check wall colliders
        Collider2D hit = Physics2D.OverlapCircle(checkPos, radius, 1 << envLayer);
        if (hit != null && !hit.isTrigger) return true;

        // 2. Check if player occupies or is stepping towards targetPos
        if (GridManager.Instance != null && GridManager.Instance.IsPositionOccupiedByPlayer(targetPos))
        {
            return true;
        }

        // 3. Check if another enemy occupies or is stepping towards targetPos
        if (GridManager.Instance != null && GridManager.Instance.IsPositionOccupiedByOtherEnemy(targetPos, this))
        {
            return true;
        }

        return false;
    }

    void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        if (!isMovingToTile)
        {
            switch (currentDirection)
            {
                case FacingDirection.Down:
                    if (idleDown != null) spriteRenderer.sprite = idleDown;
                    break;
                case FacingDirection.Up:
                    if (idleUp != null) spriteRenderer.sprite = idleUp;
                    break;
                case FacingDirection.Left:
                    if (idleLeft != null) spriteRenderer.sprite = idleLeft;
                    break;
                case FacingDirection.Right:
                    if (idleRight != null) spriteRenderer.sprite = idleRight;
                    break;
            }
            return;
        }

        Sprite[] currentFrames = GetCurrentFrames();
        if (currentFrames != null && currentFrames.Length > 0)
        {
            int mappedIndex = frameIndex % currentFrames.Length;
            if (currentFrames[mappedIndex] != null)
            {
                spriteRenderer.sprite = currentFrames[mappedIndex];
            }
        }
    }

    Sprite[] GetCurrentFrames()
    {
        switch (currentDirection)
        {
            case FacingDirection.Down: return walkDown;
            case FacingDirection.Up: return walkUp;
            case FacingDirection.Left: return walkLeft;
            case FacingDirection.Right: return walkRight;
            default: return walkDown;
        }
    }

    public void LoadEnemySprites(EnemyType type)
    {
        loadedType = type;
        string folderName = GetFolderForType(type);

        idleDown = LoadResSprite(folderName, "down_idle");
        idleUp = LoadResSprite(folderName, "up_idle");
        idleLeft = LoadResSprite(folderName, "left_idle");
        idleRight = LoadResSprite(folderName, "right_idle");

        walkDown = new Sprite[]
        {
            LoadResSprite(folderName, "down_walk_0"),
            idleDown,
            LoadResSprite(folderName, "down_walk_1"),
            idleDown
        };

        walkUp = new Sprite[]
        {
            LoadResSprite(folderName, "up_walk_0"),
            idleUp,
            LoadResSprite(folderName, "up_walk_1"),
            idleUp
        };

        walkLeft = new Sprite[]
        {
            LoadResSprite(folderName, "left_walk_0"),
            idleLeft,
            LoadResSprite(folderName, "left_walk_1"),
            idleLeft
        };

        walkRight = new Sprite[]
        {
            LoadResSprite(folderName, "right_walk_0"),
            idleRight,
            LoadResSprite(folderName, "right_walk_1"),
            idleRight
        };

        if (spriteRenderer != null && idleDown != null)
        {
            spriteRenderer.sprite = idleDown;
        }
    }

    private string GetFolderForType(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.AquaMale: return "Aqua_Male";
            case EnemyType.AquaFemale: return "Aqua_Female";
            case EnemyType.MagmaMale: return "Magma_Male";
            case EnemyType.MagmaFemale: return "Magma_Female";
            default: return "Aqua_Male";
        }
    }

    private Sprite LoadResSprite(string folder, string name)
    {
        return Resources.Load<Sprite>("Enemies/" + folder + "/" + name);
    }
}
