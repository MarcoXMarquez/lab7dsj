using UnityEngine;
using Pathfinding;

public class DynamicBlastDoor : MonoBehaviour
{
    [Header("Door State")]
    public bool isOpen = false;
    public KeyCode toggleKey = KeyCode.E;
    public float interactDistance = 2.8f;

    [Header("Visual Colors")]
    public Color closedColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Red locked
    public Color openColor = new Color(0.2f, 0.9f, 0.3f, 0.35f); // Green passable

    private BoxCollider2D col;
    private SpriteRenderer sr;
    private Transform playerTransform;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        FindPlayer();
        ApplyDoorState(false);
    }

    void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= interactDistance)
        {
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleDoor();
            }
        }
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        ApplyDoorState(true);
    }

    private void ApplyDoorState(bool updateGraph)
    {
        if (col != null)
        {
            col.enabled = !isOpen;
        }

        if (sr != null)
        {
            sr.color = isOpen ? openColor : closedColor;
        }

        if (updateGraph && AstarPath.active != null)
        {
            Vector3 size = new Vector3(Mathf.Abs(transform.lossyScale.x) + 2.5f, Mathf.Abs(transform.lossyScale.y) + 2.5f, 4f);
            Bounds b = new Bounds(transform.position, size);
            AstarPath.active.UpdateGraphs(b);
        }
    }

    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    public bool IsPlayerNear()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return false;
        return Vector2.Distance(transform.position, playerTransform.position) <= interactDistance;
    }
}
