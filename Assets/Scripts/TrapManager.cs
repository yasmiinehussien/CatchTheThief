// TrapManager.cs
// Place this in: Assets/Scripts/AI/
//══════════════════════════════════════════════════════════════
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

    // [Tooltip("UI script that plays the 2D white screen flash.")]
    // [SerializeField] private MemoryFlashUI memoryFlashUI;

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

    // ──  Detective & Glow ────────────────────────────────────
    [Header("Detective & Glow")]
    [Tooltip("Drag the Detective sprite from Assets/Sprites here.")]
    [SerializeField] private Sprite detectiveSprite;

    [Tooltip("How high above the trap tile the detective floats.")]
    [SerializeField] private float detectiveHeightOffset = 0.6f;

    [Tooltip("Color of the point light glow on revealed trap (orange/red).")]
    [SerializeField] private Color glowColor = new Color(1f, 0.4f, 0f);

    [Tooltip("Intensity of the point light glow.")]
    [SerializeField] private float glowIntensity = 3f;

    [Tooltip("Range of the point light glow.")]
    [SerializeField] private float glowRange = 2f;

    // ── Private State ────────────────────────────────────────────
    private GridManager gridManager;
    private Pathfinding pathfinding;

    // Tracks all spawned trap GameObjects, keyed by grid position
    private Dictionary<Vector2Int, GameObject> trapObjects = new Dictionary<Vector2Int, GameObject>();

    // Grid positions of all active (not-yet-triggered) traps
    private List<Vector2Int> activeTrapPositions = new List<Vector2Int>();

    private bool flashInProgress = false;

    // Runtime objects created during reveal — destroyed after
    private GameObject detectiveObj;
    private GameObject glowLightObj;

    // ── Unity Lifecycle ──────────────────────────────────────────
    private void Start()
{
    gridManager = GridManager.Instance;
    pathfinding = FindAnyObjectByType<Pathfinding>();

    if (treasureManager == null)
        treasureManager = FindAnyObjectByType<TreasureManager>();

    // Delay so GridManager.BuildGrid() finishes first
    StartCoroutine(PlaceTrapsAfterFragments());
}
    /*private void Awake()
    {
        gridManager = GridManager.Instance;
        pathfinding = FindAnyObjectByType<Pathfinding>();

        if (treasureManager == null)
            treasureManager = FindAnyObjectByType<TreasureManager>();

        // Shahd should handle:
        // if (memoryFlashUI == null)
        //     memoryFlashUI = FindAnyObjectByType<MemoryFlashUI>();
    }*/

    //private void Start()
    //{
        // Wait one frame so TreasureManager has finished spawning fragments
      //  StartCoroutine(PlaceTrapsAfterFragments());
    //}

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
    // Wait for BOTH GridManager and TreasureManager to be ready
    yield return new WaitUntil(() => 
        GridManager.Instance != null && 
        GridManager.Instance.grid != null &&
        treasureManager != null &&
        treasureManager.SpawnedCount > 0);

    gridManager = GridManager.Instance; // re-grab after waiting
    PlaceTraps();
}

    //private IEnumerator PlaceTrapsAfterFragments()
    //{
    //    yield return new WaitUntil(() => treasureManager.SpawnedCount > 0);
    //    PlaceTraps();
    //}

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

    private List<Vector2Int> GetFragmentPositions()
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        if (treasureManager == null) return positions;

        List<Vector2Int> candidates = treasureManager.GetSpawnedPositions();
        if (candidates != null)
            positions.AddRange(candidates);

        return positions;
    }

    private Vector2Int? FindNearestAdjacentRoadTile(Vector2Int origin, HashSet<Vector2Int> exclude)
    {
        int[] dx = { 0, 0, 1, -1 };
        int[] dz = { 1, -1, 0, 0 };

        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);

        for (int i = 0; i < 4; i++)
        {
            int nx = origin.x + dx[i];
            int nz = origin.y + dz[i];

            if (nx < 0 || nx >= cols || nz < 0 || nz >= rows) continue;
            if (gridManager.mapLayout[nz, nx] != 1) continue;

            Vector2Int candidate = new Vector2Int(nx, nz);
            if (exclude.Contains(candidate)) continue;

            return candidate;
        }

        return BFSNearestRoad(origin, exclude, 2);
    }

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

    private void SpawnTrap(Vector2Int cell)
    {
        Vector3 worldPos = gridManager.GridToWorld(cell.x, cell.y);
        worldPos.y = trapWorldY;

        GameObject trapGO = Instantiate(trapPrefab, worldPos, Quaternion.identity, transform);
        trapGO.name = $"Trap_{cell.x}_{cell.y}";

        // Tell TrapTile its own grid position (used when triggered)
        TrapTile tile = trapGO.GetComponent<TrapTile>();
        if (tile != null) tile.gridCell = cell;

        // Mark node in grid
        Node node = gridManager.grid[cell.x, cell.y];
        if (node != null) node.hasTrap = true;

        // Hide the renderer — trap is invisible until revealed
        Renderer rend = trapGO.GetComponentInChildren<Renderer>();
        if (rend != null) rend.enabled = false;

        // Store references
        trapObjects[cell] = trapGO;
        activeTrapPositions.Add(cell);
    }

    // ════════════════════════════════════════════════════════════
    //  AI FEATURE 2: MEMORY FLASH — A* TO NEAREST TRAP
    // ════════════════════════════════════════════════════════════

    private void TryActivateMemoryFlash()
    {
            if (flashInProgress)
    {
        Debug.Log("Flash already in progress — ignoring input.");
        return;
    }

   

        if (treasureManager == null) return;

        int collected = treasureManager.CollectedCount;
        if (collected < flashCost)
        {
            Debug.Log($"Memory Flash blocked: only {collected}/{flashCost} fragments collected.");
            // Show "not enough" warning — Shahd handles this UI
            var warning = FindAnyObjectByType<MemoryWarningAlert>();
            if (warning != null) warning.ShowWarning();
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("TrapManager: Player not assigned yet.");
            return;
        }

        if (activeTrapPositions.Count == 0)
        {
            Debug.Log("Memory Flash: No traps left to reveal.");
            return;
        }

         Vector2Int thiefCell = WorldToGrid(playerTransform.position);
         // TEMP: hardcode start position while no player exists
         //Vector2Int thiefCell = playerTransform != null 
         //? WorldToGrid(playerTransform.position) 
         //: new Vector2Int(0, 0);

        if (pathfinding == null)
            pathfinding = FindAnyObjectByType<Pathfinding>();

        Node nearestTrap = pathfinding.FindNearestTrap(thiefCell.x, thiefCell.y, activeTrapPositions);

        if (nearestTrap == null)
        {
            Debug.LogWarning("Memory Flash: A* could not reach any trap from current position.");
            return;
        }

        // Deduct charges
        //treasureManager.ConsumeFlashCharge(flashCost);

        // Reveal it
        StartCoroutine(RevealTrap(new Vector2Int(nearestTrap.x, nearestTrap.z)));
    }

    /// <summary>
    /// Reveals the nearest trap tile for revealDuration seconds:
    ///   1. Swaps to glowing revealMaterial
    ///   2. Spawns a Point Light above it (orange glow)
    ///   3. Spawns the detective sprite floating above, facing the camera
    ///   4. After 2 seconds — hides everything, destroys light + detective
    /// </summary>
   private IEnumerator RevealTrap(Vector2Int trapCell)
    {
        flashInProgress = true;

        // Shahd handles screen flash:
        // memoryFlashUI?.PlayScreenFlash();

        if (!trapObjects.TryGetValue(trapCell, out GameObject trapGO) || trapGO == null)
        {
            flashInProgress = false;
            yield break;
        }

        // ── 1. GLOW MATERIAL ─────────────────────────────────────
        Renderer rend = trapGO.GetComponentInChildren<Renderer>();
        Material originalMat = null;

        if (rend != null)
        {
            originalMat  = rend.material;
            rend.enabled = true;
            if (revealMaterial != null)
                rend.material = revealMaterial;
        }

        // ── 2. POINT LIGHT above the trap ────────────────────────
        glowLightObj = new GameObject("TrapGlowLight");
        glowLightObj.transform.position = new Vector3(trapCell.x, trapWorldY + 0.8f, trapCell.y);

        Light pointLight     = glowLightObj.AddComponent<Light>();
        pointLight.type      = LightType.Point;
        pointLight.color     = glowColor;
        pointLight.intensity = glowIntensity;
        pointLight.range     = glowRange;
        pointLight.shadows   = LightShadows.None;

        // ── 3. DETECTIVE SPRITE floating above the trap ──────────
        detectiveObj = new GameObject("DetectiveMarker");
        detectiveObj.transform.position = new Vector3(
            trapCell.x + 1.5f,
            trapWorldY + detectiveHeightOffset,
            trapCell.y
        );

        SpriteRenderer sr    = detectiveObj.AddComponent<SpriteRenderer>();
sr.sprite            = detectiveSprite;
sr.sortingOrder      = 10;
sr.drawMode          = SpriteDrawMode.Simple;  // no slicing, full image
sr.flipX             = false;
sr.flipY             = false;

// Scale based on sprite's actual size so nothing gets cropped
float spriteWidth    = detectiveSprite != null ? detectiveSprite.bounds.size.x : 1f;
float spriteHeight   = detectiveSprite != null ? detectiveSprite.bounds.size.y : 1f;
float desiredHeight  = 2f;  
float scaleFactor    = desiredHeight / spriteHeight;

detectiveObj.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
detectiveObj.AddComponent<FaceCamera>();

        // ── 4. CHANGE SUN TO POINT LIGHT ─────────────────────────
        GameObject sunObj = GameObject.Find("Sun");
        if (sunObj != null)
        {
            Light sunLight = sunObj.GetComponent<Light>();
            if (sunLight != null)
            {
                sunLight.type      = LightType.Point;
                sunLight.range     = 25f;
                sunLight.intensity = 3f;
                sunLight.color     = new Color(1f, 0.95f, 0.8f);
            }
        }

        Debug.Log($"Memory Flash: Revealing trap at {trapCell} for {revealDuration}s.");

        // ── 5. WAIT ───────────────────────────────────────────────
        yield return new WaitForSeconds(revealDuration);

        // Deduct fragments after reveal ends
        treasureManager.ConsumeFlashCharge(flashCost);

        // ── 6. HIDE TRAP + DETECTIVE + LIGHT ─────────────────────
        if (rend != null)
        {
            if (originalMat != null) rend.material = originalMat;
            rend.enabled = false;
        }

        if (glowLightObj != null) Destroy(glowLightObj);
        if (detectiveObj != null) Destroy(detectiveObj);

        // ── 7. RESTORE SUN TO DIRECTIONAL ────────────────────────
        GameObject sunObjAfter = GameObject.Find("Sun");
        if (sunObjAfter != null)
        {
            Light sunLightAfter = sunObjAfter.GetComponent<Light>();
            if (sunLightAfter != null)
            {
                sunLightAfter.type      = LightType.Directional;
                sunLightAfter.intensity = 1f;
                sunLightAfter.range     = 0f;
                sunLightAfter.color     = Color.white;
                Debug.Log("Sun restored to Directional.");
            }
        }

        flashInProgress = false;
    }

    // ════════════════════════════════════════════════════════════
    //  PUBLIC METHODS (called by other scripts)
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by TrapTile when the player steps on it.
    /// Removes the trap from the active list so Memory Flash won't target it again.
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