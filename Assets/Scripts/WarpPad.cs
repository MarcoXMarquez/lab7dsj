using UnityEngine;

public class WarpPad : MonoBehaviour
{
    [Header("Destination Link")]
    public WarpPad connectedPad;
    public string padId = "Portal A";
    public Color padColor = new Color(0.2f, 0.8f, 1f, 0.85f); // Glowing Cyan

    [Header("Teleport Mechanics")]
    public float teleportCooldown = 1.2f;
    private static float lastTeleportTime = -10f;

    private SpriteRenderer sr;
    private Vector3 initialScale;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
    }

    void Start()
    {
        if (sr != null)
        {
            Shader holoShader = Shader.Find("Custom/HologramCollectible");
            if (holoShader != null)
            {
                sr.material = new Material(holoShader);
            }

            sr.color = padColor;
        }
    }

    void Update()
    {
        // Gentle pulsating glow effect
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
        transform.localScale = new Vector3(initialScale.x * pulse, initialScale.y * pulse, initialScale.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.parent != null && other.transform.parent.CompareTag("Player")))
        {
            if (Time.time - lastTeleportTime < teleportCooldown) return;
            if (connectedPad == null) return;

            lastTeleportTime = Time.time;

            Transform playerTr = other.CompareTag("Player") ? other.transform : other.transform.parent;
            Rigidbody2D rb = playerTr.GetComponent<Rigidbody2D>();

            // Teleport departure spark
            CreateWarpSpark(transform.position);

            // Relocate player to destination pad
            Vector3 targetPosition = connectedPad.transform.position;
            if (rb != null)
            {
                rb.position = (Vector2)targetPosition;
            }
            playerTr.position = new Vector3(targetPosition.x, targetPosition.y, playerTr.position.z);

            // Teleport arrival spark
            CreateWarpSpark(connectedPad.transform.position);
        }
    }

    private void CreateWarpSpark(Vector3 pos)
    {
        GameObject spark = new GameObject("WarpSpark");
        spark.transform.position = pos;
        SpriteRenderer sparkSr = spark.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            sparkSr.sprite = sr.sprite;
            sparkSr.color = new Color(padColor.r, padColor.g, padColor.b, 0.9f);
        }
        spark.transform.localScale = Vector3.one * 1.5f;
        Destroy(spark, 0.4f);
    }
}
