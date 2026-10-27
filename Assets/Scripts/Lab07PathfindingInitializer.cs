using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class Lab07PathfindingInitializer : MonoBehaviour
{
    public int width = 72;
    public int depth = 48;
    public float nodeSize = 1f;

    void Start()
    {
        ScanGraph();
    }

    public void ScanGraph()
    {
        if (AstarPath.active == null || AstarPath.active.data == null) return;

        // Ensure graph type definitions are cached
        try
        {
            AstarPath.active.data.FindGraphTypes();
        }
        catch (System.Exception) {}

        GridGraph gg = null;

        // Try getting existing grid graph
        try
        {
            gg = AstarPath.active.data.gridGraph;
        }
        catch (System.Exception) {}

        if (gg == null && AstarPath.active.data.graphs != null)
        {
            foreach (var g in AstarPath.active.data.graphs)
            {
                if (g is GridGraph grid)
                {
                    gg = grid;
                    break;
                }
            }
        }

        // Create if missing
        if (gg == null)
        {
            try
            {
                gg = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Lab07PathfindingInitializer] Could not add graph yet: " + ex.Message);
            }
        }

        if (gg != null)
        {
            gg.is2D = true;
            gg.center = Vector3.zero;
            gg.SetDimensions(width, depth, nodeSize);
            gg.cutCorners = false;

            if (gg.collision == null) gg.collision = new GraphCollision();
            gg.collision.use2D = true;
            gg.collision.type = ColliderType.Sphere;
            gg.collision.diameter = 1.4f;
            gg.collision.collisionCheck = true;
            gg.collision.heightCheck = false;

            int envLayer = LayerMask.NameToLayer("Environment");
            if (envLayer == -1) envLayer = 8;
            gg.collision.mask = 1 << envLayer;
        }

        try
        {
            AstarPath.active.Scan();
            Debug.Log("[Lab07PathfindingInitializer] A* GridGraph scanned successfully: 72x48 2D nodes.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Lab07PathfindingInitializer] Scan deferred: " + ex.Message);
        }
    }
}
