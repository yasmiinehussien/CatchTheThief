// TrapManager.cs
// Place this in: Assets/Scripts/AI/
//
// ══════════════════════════════════════════════════════════════
//  WHAT THIS SCRIPT DOES (Your AI Task — Member 3):
//
//  AI Feature 1 — Smart Trap Placement
//    At game start, reads all fragment positions from TreasureManager.
//    For each fragment tile, finds the nearest adjacent road tile.
//    Spawns a hidden TrapTile prefab there.
//    → Forces every reward to have nearby danger.
//
//  AI Feature 2 — Memory Flash (A* Pathfinding)
//    On Space press (if collected >= 2 treasures):
//      → Runs A* from Thief's grid position to every trap tile.
//      → Finds the nearest reachable trap.
//      → Reveals (glows) that trap tile for 2 seconds, then hides it.
//      → Also fires a 2D screen flash (white overlay) via MemoryFlashUI.
//      → Costs the player 2 fragment charges (handled via TreasureManager).
//
// ══════════════════════════════════════════════════════════════
//
// INPUTS THIS SCRIPT NEEDS (Wire in Inspector):
//   • trapPrefab         — your TrapTile prefab (the flat hidden tile)
//   • treasureManager    — drag TreasureManager GameObject
//   • playerTransform    — drag the Thief (Player) GameObject
//   • memoryFlashUI      — drag the MemoryFlashUI GameObject (screen flash)
//   • revealDuration     — how long trap glows (default: 2 seconds)
//   • revealMaterial     — glowing red/orange material for revealed trap
//
// ══════════════════════════════════════════════════════════════

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem;
public class TrapManager : MonoBehaviour
{
    // ── Inspector References ─────────────────────────────────────
    [Header("References")]
    [Tooltip("The TrapTile prefab (hidden flat tile on road).")]
    [SerializeField] private GameObject trapPrefab;

    [Tooltip("The TreasureManager in the scene — gives us fragment positions.")]
    [SerializeField] private TreasureManager treasureManager;

    [Tooltip("The Thief (Player) transform — used to get grid position for A*.")]
    [SerializeField] private Transform playerTransform;

    //[Tooltip("UI script that plays the 2D white screen flash.")]
    //[SerializeField] private MemoryFlashUI memoryFlashUI;

    [Header("Trap Settings")]
    [Tooltip("World Y height for spawned traps (should match road tile Y).")]
    [SerializeField] private float trapWorldY = 0.02f;

    [Tooltip("How many seconds to reveal the nearest trap when Flash is used.")]
    [SerializeField] private float revealDuration = 2f;

    [Tooltip("Material applied to the trap tile when revealed (glowing/highlighted).")]
    [SerializeField] private Material revealMaterial;

    [Header("Flash Cost")]
    [Tooltip("How many fragment charges are consumed per Memory Flash use (default 2).")]
    [SerializeField, Min(1)] private int flashCost = 2;

    // ── Private State ────────────────────────────────────────────
    private GridManager gridManager;
    private Pathfinding pathfinding;

    // Tracks all spawned trap GameObjects, keyed by grid position
    private Dictionary<Vector2Int, GameObject> trapObjects = new Dictionary<Vector2Int, GameObject>();

    // Grid positions of all active (not-yet-triggered) traps
    private List<Vector2Int> activeTrapPositions = new List<Vector2Int>();

    private bool flashInProgress = false;

    // ── Unity Lifecycle ──────────────────────────────────────────
    private void Awake()
    {
        gridManager = GridManager.Instance;
        pathfinding = FindAnyObjectByType<Pathfinding>();

        if (treasureManager == null)
            treasureManager = FindAnyObjectByType<TreasureManager>();

        //Shahd should handle
        //if (memoryFlashUI == null)
        //    memoryFlashUI = FindAnyObjectByType<MemoryFlashUI>();
    }

    private void Start()
    {
        // Wait one frame so TreasureManager has finished spawning fragments
        StartCoroutine(PlaceTrapsAfterFragments());
    }

    private void Update()
    {
        // ── Memory Flash Input (Space bar) ───────────────────────
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryActivateMemoryFlash();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  AI FEATURE 1: SMART TRAP PLACEMENT
    // ════════════════════════════════════════════════════════════

    private IEnumerator PlaceTrapsAfterFragments()
    {
        // Wait 2 frames to let TreasureManager.Start() finish spawning
        yield return null;
        yield return null;

        PlaceTraps();
    }

    /// <summary>
    /// Reads fragment positions from TreasureManager zones.
    /// For each fragment, finds the nearest adjacent road tile and spawns a trap there.
    /// </summary>
    private void PlaceTraps()
    {
        if (gridManager == null || gridManager.grid == null)
        {
            Debug.LogError("TrapManager: GridManager not ready.");
            return;
        }

        if (trapPrefab == null)
        {
            Debug.LogError("TrapManager: Trap prefab not assigned!");
            return;
        }

        // Collect all fragment candidate positions from TreasureManager zones
        List<Vector2Int> fragmentPositions = GetFragmentPositions();

        if (fragmentPositions == null || fragmentPositions.Count == 0)
        {
            Debug.LogWarning("TrapManager: No fragment positions found. Cannot place traps.");
            return;
        }

        int trapsPlaced = 0;
        HashSet<Vector2Int> usedTrapCells = new HashSet<Vector2Int>();

        foreach (Vector2Int fragPos in fragmentPositions)
        {
            Vector2Int? trapCell = FindNearestAdjacentRoadTile(fragPos, usedTrapCells);

            if (trapCell == null)
            {
                Debug.LogWarning($"TrapManager: No adjacent road tile found near fragment at {fragPos}.");
                continue;
            }

            SpawnTrap(trapCell.Value);
            usedTrapCells.Add(trapCell.Value);
            trapsPlaced++;
        }

        Debug.Log($"TrapManager: Placed {trapsPlaced} traps from {fragmentPositions.Count} fragments.");
    }

    /// <summary>
    /// Gets ALL candidate fragment positions from TreasureManager's zone data.
    /// This includes candidates that weren't selected this run (to ensure enough traps).
    /// If you want only SPAWNED fragments, swap to reading active objects instead.
    /// </summary>
    private List<Vector2Int> GetFragmentPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        if (treasureManager == null) return positions;

        // Access zones via reflection-friendly public method
        // TreasureManager exposes its zones as a serialized list.
        // We read them via a helper method we'll add to TreasureManager,
        // OR we call the zone candidates directly (add GetZoneCandidates() to TreasureManager).
        List<Vector2Int> candidates = treasureManager.GetAllCandidatePositions();
        if (candidates != null)
            positions.AddRange(candidates);

        return positions;
    }

    /// <summary>
    /// Finds the nearest walkable road tile adjacent (4-directional) to the given cell.
    /// Skips cells already used for another trap.
    /// </summary>
    private Vector2Int? FindNearestAdjacentRoadTile(Vector2Int origin, HashSet<Vector2Int> exclude)
    {
        int[] dx = { 0, 0, 1, -1 };
        int[] dz = { 1, -1, 0, 0 };

        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);

        for (int i = 0; i < 4; i++)
        {
            int nx = origin.x + dx[i];
            int nz = origin.y + dz[i];  // Vector2Int.y = grid Z

            if (nx < 0 || nx >= cols || nz < 0 || nz >= rows) continue;
            if (gridManager.mapLayout[nz, nx] != 1) continue;  // must be road

            Vector2Int candidate = new Vector2Int(nx, nz);
            if (exclude.Contains(candidate)) continue;          // already has a trap

            return candidate;
        }

        // No immediate neighbour — search up to radius 2 (BFS)
        return BFSNearestRoad(origin, exclude, 2);
    }

    /// <summary>
    /// BFS fallback: searches up to `maxRadius` tiles away for a road tile.
    /// </summary>
    private Vector2Int? BFSNearestRoad(Vector2Int origin, HashSet<Vector2Int> exclude, int maxRadius)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int> { origin };
        queue.Enqueue(origin);

        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);

        int[] dx = { 0, 0, 1, -1 };
        int[] dz = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            int dist = Mathf.Abs(curr.x - origin.x) + Mathf.Abs(curr.y - origin.y);
            if (dist > maxRadius) continue;

            for (int i = 0; i < 4; i++)
            {
                int nx = curr.x + dx[i];
                int nz = curr.y + dz[i];

                if (nx < 0 || nx >= cols || nz < 0 || nz >= rows) continue;
                Vector2Int next = new Vector2Int(nx, nz);
                if (visited.Contains(next)) continue;
                visited.Add(next);

                if (gridManager.mapLayout[nz, nx] == 1 && !exclude.Contains(next))
                    return next;

                queue.Enqueue(next);
            }
        }

        return null;
    }

    /// <summary>
    /// Spawns a trap GameObject at the given grid cell. Trap is hidden (looks like road).
    /// </summary>
    private void SpawnTrap(Vector2Int cell)
    {
        Vector3 worldPos = gridManager.GridToWorld(cell.x, cell.y);
        worldPos.y = trapWorldY;

        GameObject trapGO = Instantiate(trapPrefab, worldPos, Quaternion.identity, transform);
        trapGO.name = $"Trap_{cell.x}_{cell.y}";

        // Mark node in grid
        Node node = gridManager.grid[cell.x, cell.y];
        if (node != null) node.hasTrap = true;

        // Store references
        trapObjects[cell] = trapGO;
        activeTrapPositions.Add(cell);
    }

    // ════════════════════════════════════════════════════════════
    //  AI FEATURE 2: MEMORY FLASH — A* TO NEAREST TRAP
    // ════════════════════════════════════════════════════════════

    private void TryActivateMemoryFlash()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("TrapManager: Player not assigned yet.");
            return;
        }
        if (flashInProgress)
        {
            Debug.Log("Memory Flash already in progress.");
            return;
        }

        // Check: player must have collected >= 2 fragments (flashValue >= 1.0 means 2 collected)
        // TreasureManager uses flashIncreasePerTreasure = 0.5 → 2 fragments = flashValue 1.0
        if (treasureManager == null)
        {
            Debug.LogWarning("TrapManager: TreasureManager reference missing.");
            return;
        }

        int collected = treasureManager.CollectedCount;
        if (collected < flashCost)
        {
            // Shahd's script  to show "Not enough memories!" message, shahd should handle
            //MemoryFlashUI ui = memoryFlashUI ?? FindAnyObjectByType<MemoryFlashUI>();
            //ui?.ShowNotEnoughMessage();
            Debug.Log($"Memory Flash blocked: only {collected}/{flashCost} fragments collected.");
            return;
        }

        if (activeTrapPositions.Count == 0)
        {
            Debug.Log("Memory Flash: No traps left to reveal.");
            return;
        }

        // Get thief grid position
        Vector2Int thiefCell = WorldToGrid(playerTransform.position);

        // Run A* to find nearest trap
        if (pathfinding == null)
            pathfinding = FindAnyObjectByType<Pathfinding>();

        Node nearestTrap = pathfinding.FindNearestTrap(thiefCell.x, thiefCell.y, activeTrapPositions);

        if (nearestTrap == null)
        {
            Debug.LogWarning("Memory Flash: A* could not reach any trap from current position.");
            return;
        }

        // Deduct flash cost from TreasureManager
        treasureManager.ConsumeFlashCharge(flashCost);

        // Reveal the trap tile
        StartCoroutine(RevealTrap(new Vector2Int(nearestTrap.x, nearestTrap.z)));
    }

    /// <summary>
    /// Reveals the trap tile visually for `revealDuration` seconds, then hides it again.
    /// Also triggers the 2D screen flash via MemoryFlashUI.
    /// </summary>
    private IEnumerator RevealTrap(Vector2Int trapCell)
    {
        flashInProgress = true;

        // should be handled by Shahd 
       // memoryFlashUI?.PlayScreenFlash();

        if (!trapObjects.TryGetValue(trapCell, out GameObject trapGO) || trapGO == null)
        {
            flashInProgress = false;
            yield break;
        }

        // Store original material and swap to reveal material (glowing red/orange)
        Renderer rend = trapGO.GetComponentInChildren<Renderer>();
        Material originalMat = null;

        if (rend != null && revealMaterial != null)
        {
            originalMat = rend.material;
            rend.material = revealMaterial;
        }

        // Make trap temporarily visible (enable its renderer)
        if (rend != null) rend.enabled = true;

        Debug.Log($"Memory Flash: Revealing trap at {trapCell} for {revealDuration} seconds.");

        yield return new WaitForSeconds(revealDuration);

        // Hide trap again
        if (rend != null)
        {
            if (originalMat != null) rend.material = originalMat;
            rend.enabled = false; // Hidden again — trap is still active
        }

        flashInProgress = false;
    }

    // ════════════════════════════════════════════════════════════
    //  PUBLIC METHODS (called by other scripts)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by TrapTile when the player steps on a trap.
    /// Removes the trap from the active list so it won't be targeted by future Memory Flash.
    /// </summary>
    public void OnTrapTriggered(Vector2Int cell)
    {
        activeTrapPositions.Remove(cell);
        trapObjects.Remove(cell);

        Node node = GetGridNode(cell);
        if (node != null) node.hasTrap = false;

        Debug.Log($"TrapManager: Trap at {cell} triggered and removed.");
    }

    // ════════════════════════════════════════════════════════════
    //  UTILITY
    // ════════════════════════════════════════════════════════════

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // GridToWorld is just new Vector3(x, 0, z) so reverse is floor
        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
    }

    private Node GetGridNode(Vector2Int cell)
    {
        if (gridManager == null || gridManager.grid == null) return null;
        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);
        if (cell.x < 0 || cell.x >= cols || cell.y < 0 || cell.y >= rows) return null;
        return gridManager.grid[cell.x, cell.y];
    }
}