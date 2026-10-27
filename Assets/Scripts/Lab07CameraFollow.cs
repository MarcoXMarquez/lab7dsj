using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Lab07CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Follow Dynamics (Pokemon Style)")]
    [Range(2f, 25f)]
    public float smoothSpeed = 9f;

    [Header("Camera Zoom Modes")]
    public float adventureSize = 7.5f;   // Close-up top-down adventure zoom (like Pokemon)
    public float satelliteSize = 22.0f;  // Full map overview zoom
    public KeyCode zoomToggleKey = KeyCode.Z;
    public float zoomTransitionSpeed = 6f;

    [Header("Map Boundaries Clamping")]
    public bool clampToBounds = true;
    public Vector2 minBounds = new Vector2(-36f, -24f);
    public Vector2 maxBounds = new Vector2(36f, 24f);

    private Camera cam;
    private float currentTargetSize;
    private bool isSatelliteMode = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;
            cam.orthographicSize = adventureSize;
        }
        currentTargetSize = adventureSize;
    }

    void Start()
    {
        FindPlayerIfNull();
        if (target != null)
        {
            Vector3 startPos = CalculateClampedPosition(target.position + offset);
            transform.position = startPos;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindPlayerIfNull();
            if (target == null) return;
        }

        // 1. Toggle between Adventure Zoom and Satellite Overview with [Z]
        if (Input.GetKeyDown(zoomToggleKey))
        {
            isSatelliteMode = !isSatelliteMode;
            currentTargetSize = isSatelliteMode ? satelliteSize : adventureSize;
        }

        // Smoothly interpolate camera size
        if (cam != null && Mathf.Abs(cam.orthographicSize - currentTargetSize) > 0.01f)
        {
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, currentTargetSize, zoomTransitionSpeed * Time.deltaTime);
        }

        // 2. Compute desired position following the player
        Vector3 desiredPos = target.position + offset;

        // 3. Clamp position strictly within level boundary walls
        if (clampToBounds && cam != null)
        {
            desiredPos = CalculateClampedPosition(desiredPos);
        }

        // 4. Smooth movement
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
    }

    private Vector3 CalculateClampedPosition(Vector3 targetPos)
    {
        if (cam == null) return targetPos;

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = minBounds.x + halfWidth;
        float maxX = maxBounds.x - halfWidth;
        float minY = minBounds.y + halfHeight;
        float maxY = maxBounds.y - halfHeight;

        float clampedX = (minX < maxX) ? Mathf.Clamp(targetPos.x, minX, maxX) : (minBounds.x + maxBounds.x) * 0.5f;
        float clampedY = (minY < maxY) ? Mathf.Clamp(targetPos.y, minY, maxY) : (minBounds.y + maxBounds.y) * 0.5f;

        return new Vector3(clampedX, clampedY, offset.z);
    }

    private void FindPlayerIfNull()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
    }

    public bool IsSatelliteMode => isSatelliteMode;
}
