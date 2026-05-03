// Pathfinding.cs
// Place this in: Assets/Scripts/AI/
//
// HOW IT WORKS:
//   FindPath(startX, startZ, targetX, targetZ)
//     → Returns a List<Node> from start to target using A* on the GridManager grid.
//     → Returns null if no path exists.
//
// USED BY: TrapManager.cs (MemoryFlash finds nearest trap via A*)

using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    public static Pathfinding Instance { get; private set; }

    private GridManager gridManager;

    private void Awake()
    {
        Instance = this;
        gridManager = GridManager.Instance;
    }

    // ────────────────────────────────────────────────────────────
    //  PUBLIC: Find shortest path between two grid positions
    // ────────────────────────────────────────────────────────────
    /// <summary>
    /// Finds A* path from (startX,startZ) to (targetX,targetZ).
    /// Returns null if no path found or grid not ready.
    /// </summary>
    public List<Node> FindPath(int startX, int startZ, int targetX, int targetZ)
    {
        if (gridManager == null)
            gridManager = GridManager.Instance;

        if (gridManager == null || gridManager.grid == null)
        {
            Debug.LogWarning("Pathfinding: GridManager or grid is null.");
            return null;
        }

        Node startNode = GetNode(startX, startZ);
        Node targetNode = GetNode(targetX, targetZ);

        if (startNode == null || targetNode == null)
            return null;

        // Reset all nodes before each search
        ResetGrid();

        List<Node> openSet = new List<Node> { startNode };
        HashSet<Node> closed = new HashSet<Node>();

        startNode.gCost = 0;
        startNode.hCost = Heuristic(startNode, targetNode);
        startNode.parent = null;

        while (openSet.Count > 0)
        {
            Node current = GetLowestFCost(openSet);

            if (current == targetNode)
                return RetracePath(startNode, targetNode);

            openSet.Remove(current);
            closed.Add(current);

            foreach (Node neighbour in GetNeighbours(current))
            {
                if (!neighbour.isWalkable || closed.Contains(neighbour))
                    continue;

                int tentativeG = current.gCost + 1; // All moves cost 1 (Manhattan grid)

                if (tentativeG < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = tentativeG;
                    neighbour.hCost = Heuristic(neighbour, targetNode);
                    neighbour.parent = current;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }

        // No path found
        return null;
    }

    // ────────────────────────────────────────────────────────────
    //  PUBLIC: Find nearest trap node from a starting position
    //          (Used by Memory Flash)
    // ────────────────────────────────────────────────────────────
    /// <summary>
    /// Runs A* from (startX, startZ) to EACH trap tile.
    /// Returns the Node of the nearest reachable trap (shortest path length).
    /// Returns null if no traps are reachable.
    /// </summary>
    public Node FindNearestTrap(int startX, int startZ, List<Vector2Int> trapPositions)
    {
        if (trapPositions == null || trapPositions.Count == 0)
        {
            Debug.LogWarning("Pathfinding.FindNearestTrap: no trap positions provided.");
            return null;
        }

        Node nearestTrap = null;
        int shortestLength = int.MaxValue;

        foreach (Vector2Int trapPos in trapPositions)
        {
            List<Node> path = FindPath(startX, startZ, trapPos.x, trapPos.y);
            if (path != null && path.Count < shortestLength)
            {
                shortestLength = path.Count;
                nearestTrap = GetNode(trapPos.x, trapPos.y);
            }
        }

        return nearestTrap;
    }

    // ────────────────────────────────────────────────────────────
    //  PRIVATE HELPERS
    // ────────────────────────────────────────────────────────────

    private Node GetNode(int x, int z)
    {
        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);
        if (x < 0 || x >= cols || z < 0 || z >= rows) return null;
        return gridManager.grid[x, z];
    }

    // 4-directional (no diagonals — matches the top-down city grid)
    private List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        int[][] dirs = new int[][] {
            new int[]{  0,  1 },
            new int[]{  0, -1 },
            new int[]{  1,  0 },
            new int[]{ -1,  0 }
        };
        foreach (int[] d in dirs)
        {
            Node n = GetNode(node.x + d[0], node.z + d[1]);
            if (n != null) neighbours.Add(n);
        }
        return neighbours;
    }

    private int Heuristic(Node a, Node b)
    {
        // Manhattan distance — perfect for 4-directional grid
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    private Node GetLowestFCost(List<Node> openSet)
    {
        Node best = openSet[0];
        foreach (Node n in openSet)
        {
            if (n.fCost < best.fCost || (n.fCost == best.fCost && n.hCost < best.hCost))
                best = n;
        }
        return best;
    }

    private List<Node> RetracePath(Node start, Node end)
    {
        List<Node> path = new List<Node>();
        Node current = end;
        while (current != start)
        {
            path.Add(current);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    // Reset A* cost fields on every node before a new search
    private void ResetGrid()
    {
        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);
        for (int z = 0; z < rows; z++)
            for (int x = 0; x < cols; x++)
            {
                Node n = gridManager.grid[x, z];
                if (n == null) continue;
                n.gCost = int.MaxValue;
                n.hCost = 0;
                n.parent = null;
            }
    }
}