using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    public enum ItemType
    {
        IntelDisk,
        SpeedBoost,
        EMPCharge,
        Medkit
    }

    [Header("Item Config")]
    public ItemType itemType = ItemType.IntelDisk;
    public int scoreValue = 100;
    public float floatAmplitude = 0.15f;
    public float floatFrequency = 3f;
    public float rotationSpeed = 60f;

    private Vector3 initialPosition;
    private float timeOffset;
    private SpriteRenderer sr;

    void Start()
    {
        initialPosition = transform.position;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            Shader holoShader = Shader.Find("Custom/HologramCollectible");
            if (holoShader != null)
            {
                sr.material = new Material(holoShader);
            }

            switch (itemType)
            {
                case ItemType.IntelDisk:
                    sr.color = new Color(0.2f, 0.9f, 1f, 1f); // Glowing Cyan
                    break;
                case ItemType.SpeedBoost:
                    sr.color = new Color(1f, 0.85f, 0.2f, 1f); // Glowing Gold/Yellow
                    break;
                case ItemType.EMPCharge:
                    sr.color = new Color(0.4f, 1f, 0.4f, 1f); // Glowing Emerald Green
                    break;
                case ItemType.Medkit:
                    sr.color = new Color(1f, 0.25f, 0.4f, 1f); // Glowing Ruby Red/Pink
                    break;
            }
        }
    }

    void Update()
    {
        // Gentle bobbing and rotating animation
        float newY = initialPosition.y + Mathf.Sin(Time.time * floatFrequency + timeOffset) * floatAmplitude;
        transform.position = new Vector3(initialPosition.x, newY, initialPosition.z);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || (other.transform.parent != null && other.transform.parent.CompareTag("Player")))
        {
            if (Lab07GameManager.Instance != null)
            {
                Lab07GameManager.Instance.OnItemCollected(this);
            }

            // Spawn floating text / particle effect effect
            CreatePickupSpark();

            Destroy(gameObject);
        }
    }

    private void CreatePickupSpark()
    {
        GameObject spark = new GameObject("PickupSpark");
        spark.transform.position = transform.position;
        SpriteRenderer sparkSr = spark.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            sparkSr.sprite = sr.sprite;
            sparkSr.color = sr.color;
        }
        spark.transform.localScale = Vector3.one * 0.8f;
        Destroy(spark, 0.35f);
    }
}
