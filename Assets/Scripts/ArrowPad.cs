using UnityEngine;

public class ArrowPad : MonoBehaviour
{
    public enum Direction
    {
        Left,
        Right,
        Up,
        Down
    }

    public Direction direction = Direction.Left;

    public Vector2 DirectionVector
    {
        get
        {
            switch (direction)
            {
                case Direction.Left: return Vector2.left;
                case Direction.Right: return Vector2.right;
                case Direction.Up: return Vector2.up;
                case Direction.Down: return Vector2.down;
                default: return Vector2.left;
            }
        }
    }

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
            player.TriggerArrowPad(DirectionVector, transform.position);
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

        // Thick black direction arrow
        Vector3 dir = (Vector3)DirectionVector * (Mathf.Min(size.x, size.y) * 0.4f);
        Vector3 start = center - dir * 0.5f;
        Vector3 end = center + dir * 0.5f;
        UnityEditor.Handles.DrawAAPolyLine(3.5f, start, end);

        Vector3 right = Vector3.Cross(dir.normalized, Vector3.forward) * (Mathf.Min(size.x, size.y) * 0.2f);
        Vector3 head1 = end - dir.normalized * (Mathf.Min(size.x, size.y) * 0.25f) + right;
        Vector3 head2 = end - dir.normalized * (Mathf.Min(size.x, size.y) * 0.25f) - right;
        UnityEditor.Handles.DrawAAPolyLine(3.5f, head1, end, head2);
    }
#endif
}
