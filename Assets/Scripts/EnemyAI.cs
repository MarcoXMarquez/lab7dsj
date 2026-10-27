using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum Personality
    {
        Blinky_Direct,    // Magma Male: Relentless direct pursuit (exact tile of player)
        Pinky_Ambusher,   // Aqua Female: Predicts 3 tiles ahead in Red's facing direction
        Inky_Flanker,     // Aqua Male: Flanks from the side / pincer movement
        Clyde_Wanderer    // Magma Female: Chases if far (>5.5 tiles), retreats if close (<=5.5 tiles)
    }

    [Header("Pac-Man Ghost Personality")]
    public Personality personality = Personality.Blinky_Direct;
    public Transform target;
    public Vector2 homeCorner = new Vector2(5.02f, -6.91f); // Bottom-right corridor for Clyde retreat

    [Header("Step Timing")]
    public float stepDelay = 0.05f; // Slight pause between tile steps (in seconds)

    private EnemyMovement enemyMovement;
    private PlayerScript playerScript;
    private float delayTimer = 0f;

    void Awake()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        if (enemyMovement == null)
        {
            enemyMovement = GetComponentInChildren<EnemyMovement>();
        }
    }

    void Start()
    {
        FindPlayer();

        if (enemyMovement != null)
        {
            enemyMovement.OnStepFinished += OnStepFinished;
        }

        DecideNextStep();
    }

    void OnDestroy()
    {
        if (enemyMovement != null)
        {
            enemyMovement.OnStepFinished -= OnStepFinished;
        }
    }

    void Update()
    {
        if (target == null)
        {
            FindPlayer();
            if (target == null) return;
        }

        if (enemyMovement == null) return;

        // If idle on a tile, countdown delay and decide next step
        if (!enemyMovement.isMovingToTile)
        {
            delayTimer += Time.deltaTime;
            if (delayTimer >= stepDelay)
            {
                delayTimer = 0f;
                DecideNextStep();
            }
        }
    }

    private void OnStepFinished()
    {
        delayTimer = 0f;
        DecideNextStep();
    }

    public void DecideNextStep()
    {
        if (enemyMovement == null || enemyMovement.isMovingToTile) return;
        if (target == null) FindPlayer();
        if (target == null || GridManager.Instance == null) return;

        Vector2 targetWorldPos = CalculatePersonalityTarget();

        // Get immediate next cardinal direction from BFS
        Vector2? nextDir = GridManager.Instance.GetNextCardinalDirection(transform.position, targetWorldPos, enemyMovement);

        if (nextDir.HasValue)
        {
            enemyMovement.TryStepInDirection(nextDir.Value);
        }
    }

    private Vector2 CalculatePersonalityTarget()
    {
        Vector2 playerPos = target.position;

        switch (personality)
        {
            case Personality.Blinky_Direct:
                return playerPos;

            case Personality.Pinky_Ambusher:
                if (playerScript != null)
                {
                    return playerPos + playerScript.FacingVector * 3f;
                }
                return playerPos;

            case Personality.Inky_Flanker:
                if (playerScript != null)
                {
                    Vector2 facing = playerScript.FacingVector;
                    Vector2 flank = new Vector2(-facing.y, facing.x) * 2f;
                    return playerPos + flank;
                }
                return playerPos;

            case Personality.Clyde_Wanderer:
                float dist = Vector2.Distance(transform.position, playerPos);
                if (dist > 5.5f)
                {
                    return playerPos;
                }
                else
                {
                    return homeCorner;
                }

            default:
                return playerPos;
        }
    }

    private void FindPlayer()
    {
        if (target != null)
        {
            if (playerScript == null) playerScript = target.GetComponent<PlayerScript>();
            return;
        }

        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo == null) playerGo = GameObject.Find("Player");
        if (playerGo != null)
        {
            target = playerGo.transform;
            playerScript = playerGo.GetComponent<PlayerScript>();
        }
    }
}
