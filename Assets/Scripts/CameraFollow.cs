using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;

    [Header("Follow Settings")]
    public bool smoothFollow = true;
    [Range(1f, 30f)]
    public float smoothSpeed = 12f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Map Bounds Clamping (No Black Borders)")]
    public bool clampToBounds = true;
    public SpriteRenderer mapRenderer;
    public Vector2 minBounds = new Vector2(-15.5f, -11.0f);
    public Vector2 maxBounds = new Vector2(15.5f, 11.0f);

    private Camera cam;

    void Awake()
    {
        EnsureCamera();
        InitializeMapBounds();
    }

    void Start()
    {
        EnsureCamera();
        InitializeMapBounds();
        FindPlayerIfNull();

        // Snap directly to player on start
        if (target != null)
        {
            Vector3 desiredPos = target.position + offset;
            if (clampToBounds && cam != null)
            {
                desiredPos = ClampCamera(desiredPos);
            }
            transform.position = desiredPos;
        }
    }

    void LateUpdate()
    {
        EnsureCamera();

        if (target == null)
        {
            FindPlayerIfNull();
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;

        if (clampToBounds && cam != null)
        {
            desiredPosition = ClampCamera(desiredPosition);
        }

        if (smoothFollow)
        {
            // If very far, snap instantly to avoid dragging
            if (Vector3.Distance(transform.position, desiredPosition) > 6f)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            }
        }
        else
        {
            transform.position = desiredPosition;
        }

        // Final safety clamp so interpolation never crosses map edge
        if (clampToBounds && cam != null)
        {
            transform.position = ClampCamera(transform.position);
        }
    }

    private void EnsureCamera()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 5.0f; // Authentic GBA 10-tile vertical zoom
            }
        }
    }

    public void InitializeMapBounds()
    {
        if (mapRenderer == null)
        {
            GameObject mapObj = GameObject.Find("B2F_Map");
            if (mapObj != null)
            {
                mapRenderer = mapObj.GetComponent<SpriteRenderer>();
            }
        }

        if (mapRenderer != null)
        {
            Bounds b = mapRenderer.bounds;
            minBounds = new Vector2(b.min.x, b.min.y);
            maxBounds = new Vector2(b.max.x, b.max.y);
        }
    }

    public Vector3 ClampCamera(Vector3 targetPos)
    {
        if (cam == null) return targetPos;

        float vertExtent = cam.orthographicSize;
        float horzExtent = vertExtent * cam.aspect;

        float clampedX = targetPos.x;
        float clampedY = targetPos.y;

        // X clamping
        float minX = minBounds.x + horzExtent;
        float maxX = maxBounds.x - horzExtent;
        if (minX <= maxX)
        {
            clampedX = Mathf.Clamp(targetPos.x, minX, maxX);
        }
        else
        {
            // Map is narrower than camera view
            clampedX = (minBounds.x + maxBounds.x) * 0.5f;
        }

        // Y clamping
        float minY = minBounds.y + vertExtent;
        float maxY = maxBounds.y - vertExtent;
        if (minY <= maxY)
        {
            clampedY = Mathf.Clamp(targetPos.y, minY, maxY);
        }
        else
        {
            // Map is shorter than camera view
            clampedY = (minBounds.y + maxBounds.y) * 0.5f;
        }

        return new Vector3(clampedX, clampedY, offset.z);
    }

    private void FindPlayerIfNull()
    {
        if (target != null) return;

        GameObject playerGo = GameObject.FindWithTag("Player");
        if (playerGo != null)
        {
            target = playerGo.transform;
            return;
        }

        playerGo = GameObject.Find("Player");
        if (playerGo != null)
        {
            target = playerGo.transform;
            return;
        }

        PlayerScript ps = Object.FindFirstObjectByType<PlayerScript>();
        if (ps != null)
        {
            target = ps.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw map bounds in green
        Gizmos.color = Color.green;
        Vector3 center = (minBounds + maxBounds) * 0.5f;
        Vector3 size = (maxBounds - minBounds);
        Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 0f));

        // Draw camera clamp center limits in yellow
        if (cam != null || (cam = GetComponent<Camera>()) != null)
        {
            float vertExtent = cam.orthographicSize;
            float horzExtent = vertExtent * cam.aspect;
            float minX = minBounds.x + horzExtent;
            float maxX = maxBounds.x - horzExtent;
            float minY = minBounds.y + vertExtent;
            float maxY = maxBounds.y - vertExtent;

            if (minX <= maxX && minY <= maxY)
            {
                Gizmos.color = Color.yellow;
                Vector3 clampCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
                Vector3 clampSize = new Vector3(maxX - minX, maxY - minY, 0f);
                Gizmos.DrawWireCube(clampCenter, clampSize);
            }
        }
    }
}
