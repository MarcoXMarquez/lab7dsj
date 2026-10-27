using UnityEngine;

public class StopPad : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Strictly ignore ANY enemy under all circumstances
        if (other == null || other.gameObject == null) return;
        string goName = other.gameObject.name;
        if (goName.StartsWith("Enemy") ||
            other.GetComponent<EnemyMovement>() != null ||
            other.GetComponentInParent<EnemyMovement>() != null)
        {
            return;
        }

        // 2. Only allow the Player
        if (!other.CompareTag("Player") && !goName.Equals("Player")) return;

        PlayerScript player = other.GetComponent<PlayerScript>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerScript>();
        }

        if (player != null)
        {
            player.TriggerStopPad(transform.position);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        Vector3 center = transform.position;
        Vector2 size = box != null ? box.size : new Vector2(0.6f, 0.6f);
        if (box != null)
        {
            center += (Vector3)box.offset;
        }

        // Black wire box fallback
        Gizmos.color = Color.black;
        Gizmos.DrawWireCube(center, size);

        // Thick black anti-aliased gizmo outline
        UnityEditor.Handles.color = Color.black;

        float hw = size.x * 0.5f;
        float hh = size.y * 0.5f;

        Vector3 p0 = center + new Vector3(-hw, -hh, 0f);
        Vector3 p1 = center + new Vector3(hw, -hh, 0f);
        Vector3 p2 = center + new Vector3(hw, hh, 0f);
        Vector3 p3 = center + new Vector3(-hw, hh, 0f);

        UnityEditor.Handles.DrawAAPolyLine(3.5f, p0, p1, p2, p3, p0);

        // Inner stop diamond indicator in black
        Vector3 top = center + new Vector3(0f, hh * 0.5f, 0f);
        Vector3 right = center + new Vector3(hw * 0.5f, 0f, 0f);
        Vector3 bottom = center + new Vector3(0f, -hh * 0.5f, 0f);
        Vector3 left = center + new Vector3(-hw * 0.5f, 0f, 0f);
        UnityEditor.Handles.DrawAAPolyLine(3.0f, top, right, bottom, left, top);
    }
#endif
}
