// Node.cs — create this as a new script file
using UnityEngine;

public class Node
{
    public int x, z;
    public Vector3 worldPos;
    public bool isWalkable;

    // A* fields for Member 3 (Nermeen)
    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;
    public Node parent;

    public Node(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}