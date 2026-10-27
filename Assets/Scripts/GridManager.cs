using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Boundaries (B2F Rocket Hideout)")]
    public float minX = -15f;
    public float maxX = 15f;
    public float minY = -11f;
    public float maxY = 11f;

    [Header("Collision Layer")]
    public LayerMask obstacleLayer;

    private readonly List<EnemyMovement> activeEnemies = new List<EnemyMovement>();
    private Transform playerTransform;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (obstacleLayer.value == 0)
        {
            int envLayer = LayerMask.NameToLayer("Environment");
            if (envLayer == -1) envLayer = 8;
            obstacleLayer = 1 << envLayer;
        }

        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    public void RegisterEnemy(EnemyMovement enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(EnemyMovement enemy)
    {
        activeEnemies.Remove(enemy);
    }

    public bool IsPositionOccupiedByOtherEnemy(Vector2 targetPos, EnemyMovement self)
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement other = activeEnemies[i];
            if (other == null || other == self) continue;

            if (Vector2.Distance(other.transform.position, targetPos) < 0.75f ||
                Vector2.Distance(other.TargetPosition, targetPos) < 0.75f)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsPositionOccupiedByAnyEnemy(Vector2 targetPos)
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyMovement enemy = activeEnemies[i];
            if (enemy == null) continue;

            if (Vector2.Distance(enemy.transform.position, targetPos) < 0.75f ||
                Vector2.Distance(enemy.TargetPosition, targetPos) < 0.75f)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsPositionOccupiedByPlayer(Vector2 targetPos)
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return false;

        PlayerScript player = playerTransform.GetComponent<PlayerScript>();
        Vector2 pPos = playerTransform.position;
        Vector2 pTarget = player != null ? player.TargetPosition : pPos;

        return Vector2.Distance(pPos, targetPos) < 0.75f || Vector2.Distance(pTarget, targetPos) < 0.75f;
    }

    /// <summary>
    /// Finds the best cardinal direction (Up, Down, Left, Right) to step towards target using BFS.
    /// Does NOT modify or snap any positions.
    /// </summary>
    public Vector2? GetNextCardinalDirection(Vector2 currentWorldPos, Vector2 targetWorldPos, EnemyMovement self)
    {
        Vector2Int startCell = new Vector2Int(Mathf.RoundToInt(currentWorldPos.x), Mathf.RoundToInt(currentWorldPos.y));
        Vector2Int goalCell = new Vector2Int(Mathf.RoundToInt(targetWorldPos.x), Mathf.RoundToInt(targetWorldPos.y));

        if (startCell == goalCell) return null;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startCell);
        visited.Add(startCell);

        Vector2Int[] dirs = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        Vector2Int closestCell = startCell;
        float closestDist = Vector2Int.Distance(startCell, goalCell);
        bool foundPath = false;

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();

            if (curr == goalCell)
            {
                foundPath = true;
                break;
            }

            // Prefer directions closer to goal
            List<Vector2Int> sortedDirs = new List<Vector2Int>(dirs);
            sortedDirs.Sort((a, b) =>
            {
                float da = Vector2.Distance((Vector2)(curr + a), (Vector2)goalCell);
                float db = Vector2.Distance((Vector2)(curr + b), (Vector2)goalCell);
                return da.CompareTo(db);
            });

            for (int i = 0; i < sortedDirs.Count; i++)
            {
                Vector2Int nxt = curr + sortedDirs[i];

                if (!visited.Contains(nxt))
                {
                    // Check if candidate position is physically blocked for this character
                    Vector2 worldCheck = currentWorldPos + (Vector2)(nxt - startCell);
                    if (self != null && self.IsPositionBlocked(worldCheck))
                    {
                        continue;
                    }

                    // Avoid stepping onto other enemies
                    if (curr == startCell && IsPositionOccupiedByOtherEnemy(worldCheck, self))
                    {
                        continue;
                    }

                    visited.Add(nxt);
                    parentMap[nxt] = curr;
                    queue.Enqueue(nxt);

                    float dist = Vector2.Distance((Vector2)nxt, (Vector2)goalCell);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestCell = nxt;
                    }
                }
            }
        }

        Vector2Int destination = foundPath ? goalCell : closestCell;
        if (destination == startCell) return null;

        // Backtrack to find the first step from startCell
        Vector2Int step = destination;
        while (parentMap.ContainsKey(step) && parentMap[step] != startCell)
        {
            step = parentMap[step];
        }

        Vector2Int delta = step - startCell;
        return new Vector2(delta.x, delta.y);
    }
}
