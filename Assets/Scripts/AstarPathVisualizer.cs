using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

[RequireComponent(typeof(Seeker))]
public class AstarPathVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public bool enableLineRenderer = true;
    public Color pathColor = new Color(1f, 0.45f, 0.1f, 0.85f); // High-visibility Orange
    public float lineWidth = 0.12f;

    [Header("Real-time Metrics")]
    public int waypointCount = 0;
    public float calculatedPathDistance = 0f;
    public float directEuclideanDistance = 0f;
    public float evaluationFunctionF = 0f;

    private Seeker seeker;
    private AIPath aiPath;
    private LineRenderer lineRenderer;
    private Path currentPath;

    void Awake()
    {
        seeker = GetComponent<Seeker>();
        aiPath = GetComponent<AIPath>();

        if (enableLineRenderer)
        {
            SetupLineRenderer();
        }
    }

    void OnEnable()
    {
        if (seeker != null)
        {
            seeker.pathCallback += OnPathComplete;
        }
    }

    void OnDisable()
    {
        if (seeker != null)
        {
            seeker.pathCallback -= OnPathComplete;
        }
    }

    private void SetupLineRenderer()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.5f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = pathColor;
        lineRenderer.endColor = new Color(pathColor.r, pathColor.g, pathColor.b, 0.3f);
        lineRenderer.sortingOrder = 5;
        lineRenderer.useWorldSpace = true;
    }

    public void OnPathComplete(Path p)
    {
        if (p == null || p.error) return;

        currentPath = p;
        List<Vector3> vectorPath = p.vectorPath;
        if (vectorPath == null || vectorPath.Count == 0) return;

        waypointCount = vectorPath.Count;

        // Calculate path distance g(n)
        calculatedPathDistance = 0f;
        for (int i = 0; i < vectorPath.Count - 1; i++)
        {
            calculatedPathDistance += Vector3.Distance(vectorPath[i], vectorPath[i + 1]);
        }

        // Direct distance h(n)
        directEuclideanDistance = Vector3.Distance(transform.position, vectorPath[vectorPath.Count - 1]);

        // A* Evaluation Function: f(n) = g(n) + h(n)
        evaluationFunctionF = calculatedPathDistance + directEuclideanDistance;

        // Update line renderer
        if (enableLineRenderer && lineRenderer != null)
        {
            lineRenderer.positionCount = vectorPath.Count;
            for (int i = 0; i < vectorPath.Count; i++)
            {
                // Lift slightly in z for 2D sorting
                lineRenderer.SetPosition(i, new Vector3(vectorPath[i].x, vectorPath[i].y, -0.1f));
            }
        }
    }

    void Update()
    {
        // Keep line renderer updated with current agent position
        if (enableLineRenderer && lineRenderer != null && currentPath != null && currentPath.vectorPath != null && currentPath.vectorPath.Count > 0)
        {
            if (lineRenderer.positionCount > 0)
            {
                lineRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, -0.1f));
            }
        }
    }

    void OnDrawGizmos()
    {
        if (currentPath == null || currentPath.vectorPath == null) return;

        Gizmos.color = pathColor;
        List<Vector3> points = currentPath.vectorPath;
        for (int i = 0; i < points.Count - 1; i++)
        {
            Gizmos.DrawLine(points[i], points[i + 1]);
            Gizmos.DrawWireSphere(points[i], 0.15f);
        }
    }
}
