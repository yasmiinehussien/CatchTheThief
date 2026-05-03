

using UnityEngine;

public class Node
{
    public int x, z;           // Grid coordinates
    public Vector3 worldPos;   // World position (optional, for debug/gizmos)
    public bool isWalkable;    // True = road tile, False = grass/wall

    // ── A* cost fields ──────────────────────────────────────────
    public int gCost;          // Cost from start node to this node
    public int hCost;          // Heuristic: estimated cost to target
    public int fCost => gCost + hCost;  // Total score (lower = better)

    public Node parent;        // Used to reconstruct the path

    // ── Extra flags used by TrapManager ─────────────────────────
    public bool hasTrap;       // True = a hidden trap is on this tile
    public bool isFragment;    // True = a treasure fragment is here (used for placement)

    public Node(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}