using System;
using System.Collections.Generic;
using UnityEngine;

public class TreasureManager : MonoBehaviour
{
    [Serializable]
    public class ZoneCandidates
    {
        public string zoneName;
        public List<Vector2Int> candidateCells = new List<Vector2Int>();
    }

    [Header("References")]
    [Tooltip("Grid source used to validate road cells for treasure placement.")]
    [SerializeField] private GridManager gridManager;
    [Tooltip("Prefab that gets spawned as treasure chest.")]
    [SerializeField] private GameObject chestPrefab;
    [Tooltip("Optional parent for spawned chests. Leave empty to spawn at scene root.")]
    [SerializeField] private Transform chestParent;
    [Tooltip("HUD script that updates Treasure text and Memory Flash slider.")]
    [SerializeField] private TreasureHUDController hudController;

    [Header("Spawn Rules")]
    [Tooltip("How many chests to spawn from each zone per run.")]
    [SerializeField, Min(1)] private int treasuresPerZone = 2;
    [Tooltip("World Y position used for all spawned chests.")]
    [SerializeField] private float fixedWorldY = 0.55f;
    [Tooltip("Minimum tile distance between any two spawned chests.")]
    [SerializeField, Min(0f)] private float minDistanceBetweenTreasures = 2f;
    [Tooltip("Candidate road cells grouped by zone (e.g., South/Middle/North).")]
    [SerializeField] private List<ZoneCandidates> zones = new List<ZoneCandidates>();

    [Header("Placement")]
    [Tooltip("Randomize each chest starting Y angle while keeping fixed X/Z tilt.")]
    [SerializeField] private bool randomStartYaw = false;
    [Tooltip("Fixed X tilt applied to each spawned chest.")]
    [SerializeField] private float fixedXRotation = -90f;
    [Tooltip("Fixed Z tilt applied to each spawned chest.")]
    [SerializeField] private float fixedZRotation = 0f;
    [Tooltip("Y-axis spin speed in degrees per second.")]
    [SerializeField] private float spinSpeed = 75f;

    [Header("Flash Rules")]
    [Tooltip("How much the Memory Flash bar increases per collected chest.")]
    [SerializeField, Range(0.01f, 1f)] private float flashIncreasePerTreasure = 0.5f;

    private readonly List<TreasureCollectible> activeTreasures = new List<TreasureCollectible>();
    private int collectedCount;
    private int spawnedCount;
    private float flashValue;

    public int CollectedCount => collectedCount;
    public int SpawnedCount => spawnedCount;
    public float FlashValue => flashValue;

    public event Action<int, int, float> OnProgressChanged;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = GridManager.Instance;

        if (hudController == null)
            hudController = FindAnyObjectByType<TreasureHUDController>();

        EnsureDefaultZones();
    }

    private void Start()
    {

        SpawnTreasuresForRun();
        PushHud();
    }

    public void SpawnTreasuresForRun()
    {
        CleanupActiveTreasures();

        if (!CanSpawnTreasures())
            return;

        List<ZoneCandidates> shuffledZones = new List<ZoneCandidates>(zones);
        Shuffle(shuffledZones);
        List<Vector2Int> selectedCells = new List<Vector2Int>();

        foreach (ZoneCandidates zone in shuffledZones)
        {
            if (zone == null || zone.candidateCells == null || zone.candidateCells.Count == 0)
                continue;

            List<Vector2Int> validCandidates = GetValidCandidates(zone.candidateCells);
            Shuffle(validCandidates);

            int pickedForZone = 0;
            foreach (Vector2Int candidate in validCandidates)
            {
                if (pickedForZone >= treasuresPerZone)
                    break;

                if (!IsFarEnoughFromExisting(candidate, selectedCells))
                    continue;

                SpawnAtCell(candidate);
                selectedCells.Add(candidate);
                pickedForZone++;
            }

            if (pickedForZone < treasuresPerZone)
                Debug.LogWarning($"TreasureManager: Zone '{zone.zoneName}' could only place {pickedForZone}/{treasuresPerZone} treasures with current spacing.");
        }
    }

    public void RegisterCollected(TreasureCollectible collectible)
    {
        if (collectible == null)
            return;

        collectedCount++;
        flashValue = Mathf.Clamp01(flashValue + flashIncreasePerTreasure);

        activeTreasures.Remove(collectible);

        PushHud();
    }

    [ContextMenu("Load Default Zone Candidates")]
    public void LoadDefaultZoneCandidates()
    {
        zones = new List<ZoneCandidates>
        {
            new ZoneCandidates
            {
                zoneName = "South",
                candidateCells = new List<Vector2Int>
                {
                    new Vector2Int(2, 0),
                    new Vector2Int(0, 2),
                    new Vector2Int(3, 1),
                    new Vector2Int(1, 3),
                    new Vector2Int(9, 2)
                }
            },
            new ZoneCandidates
            {
                zoneName = "Middle",
                candidateCells = new List<Vector2Int>
                {
                    new Vector2Int(3, 5),
                    new Vector2Int(6, 5),
                    new Vector2Int(1, 6),
                    new Vector2Int(8, 6),
                    new Vector2Int(11, 5)
                }
            },
            new ZoneCandidates
            {
                zoneName = "North",
                candidateCells = new List<Vector2Int>
                {
                    new Vector2Int(3, 8),
                    new Vector2Int(6, 8),
                    new Vector2Int(3, 10),
                    new Vector2Int(5, 10),
                    new Vector2Int(5, 11)
                }
            }
        };
    }

    private void EnsureDefaultZones()
    {
        if (HasAnyZoneCandidates())
            return;

        LoadDefaultZoneCandidates();
    }

    private bool HasAnyZoneCandidates()
    {
        if (zones == null || zones.Count == 0)
            return false;

        foreach (ZoneCandidates zone in zones)
        {
            if (zone != null && zone.candidateCells != null && zone.candidateCells.Count > 0)
                return true;
        }

        return false;
    }

    private void SpawnAtCell(Vector2Int cell)
    {
        Vector3 worldPos = gridManager.GridToWorld(cell.x, cell.y);
        worldPos.y = fixedWorldY;

        float startYaw = randomStartYaw ? UnityEngine.Random.Range(0f, 360f) : 0f;
        Quaternion spawnRotation = Quaternion.Euler(fixedXRotation, startYaw, fixedZRotation);

        // Instantiate without parent to avoid inheriting parent's rotation/scale.
        GameObject chestInstance = Instantiate(chestPrefab);
        if (chestInstance == null)
            return;

        // Force world position and rotation (respecting fixed X/Z and start yaw). This avoids
        // parent transform affecting the chest's world rotation (so Z stays at fixedZRotation).
        chestInstance.transform.SetPositionAndRotation(worldPos, spawnRotation);

        // Parent after setting world transform so local transform stays correct.
        if (chestParent != null)
            chestInstance.transform.SetParent(chestParent, true);

        TreasureCollectible collectible = GetOrAddCollectible(chestInstance);
        if (collectible == null)
            return;

        collectible.Initialize(this, chestInstance);

        TreasureSpin spin = chestInstance.GetComponent<TreasureSpin>();
        if (spin == null)
            spin = chestInstance.AddComponent<TreasureSpin>();

        spin.Configure(spinSpeed, fixedWorldY, fixedXRotation, fixedZRotation);

        activeTreasures.Add(collectible);
        spawnedCount++;
    }

    private bool CanSpawnTreasures()
    {
        if (gridManager == null)
        {
            Debug.LogError("TreasureManager: GridManager reference is missing.");
            return false;
        }

        if (chestPrefab == null)
        {
            Debug.LogError("TreasureManager: Chest prefab is missing.");
            return false;
        }

        if (gridManager.mapLayout == null)
        {
            Debug.LogError("TreasureManager: GridManager mapLayout is missing.");
            return false;
        }

        return true;
    }

    private static TreasureCollectible GetOrAddCollectible(GameObject chestInstance)
    {
        TreasureCollectible collectible = chestInstance.GetComponent<TreasureCollectible>();
        if (collectible != null)
            return collectible;

        collectible = chestInstance.AddComponent<TreasureCollectible>();
        if (collectible != null)
            return collectible;

        Collider childCollider = chestInstance.GetComponentInChildren<Collider>();
        if (childCollider == null)
            return null;

        GameObject host = childCollider.gameObject;
        collectible = host.GetComponent<TreasureCollectible>();
        if (collectible == null)
            collectible = host.AddComponent<TreasureCollectible>();

        return collectible;
    }

    private bool IsFarEnoughFromExisting(Vector2Int candidate, List<Vector2Int> selectedCells)
    {
        if (minDistanceBetweenTreasures <= 0f)
            return true;

        foreach (Vector2Int selectedCell in selectedCells)
        {
            float distance = Vector2Int.Distance(candidate, selectedCell);
            if (distance < minDistanceBetweenTreasures)
                return false;
        }

        return true;
    }

    private List<Vector2Int> GetValidCandidates(List<Vector2Int> candidates)
    {
        List<Vector2Int> valid = new List<Vector2Int>();

        if (gridManager == null || gridManager.mapLayout == null)
            return valid;

        HashSet<Vector2Int> seen = new HashSet<Vector2Int>();
        int rows = gridManager.mapLayout.GetLength(0);
        int cols = gridManager.mapLayout.GetLength(1);

        foreach (Vector2Int cell in candidates)
        {
            bool inBounds = cell.x >= 0 && cell.x < cols && cell.y >= 0 && cell.y < rows;
            if (!inBounds)
                continue;

            bool isRoad = gridManager.mapLayout[cell.y, cell.x] == 1;
            if (!isRoad)
                continue;

            if (seen.Add(cell))
                valid.Add(cell);
        }

        return valid;
    }

    private void CleanupActiveTreasures()
    {
        for (int index = 0; index < activeTreasures.Count; index++)
        {
            if (activeTreasures[index] != null)
                Destroy(activeTreasures[index].gameObject);
        }

        activeTreasures.Clear();
        collectedCount = 0;
        spawnedCount = 0;
        flashValue = 0f;
    }

    private void PushHud()
    {
        if (hudController == null)
            hudController = FindAnyObjectByType<TreasureHUDController>(FindObjectsInactive.Include);

        if (hudController != null)
            hudController.Render(collectedCount, flashValue);

        OnProgressChanged?.Invoke(collectedCount, spawnedCount, flashValue);
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (int index = list.Count - 1; index > 0; index--)
        {
            int swapIndex = UnityEngine.Random.Range(0, index + 1);
            (list[index], list[swapIndex]) = (list[swapIndex], list[index]);
        }
    }

    // Called by TrapManager to get all zone candidate positions
    //public List<Vector2Int> GetAllCandidatePositions()
    //{
    //    List<Vector2Int> all = new List<Vector2Int>();
    //    if (zones == null) return all;
    //    foreach (ZoneCandidates zone in zones)
    //    {
    //        if (zone == null || zone.candidateCells == null) continue;
    //        all.AddRange(zone.candidateCells);
    //    }
    //    return all;
    //}


    public List<Vector2Int> GetSpawnedPositions()
    {
        List<Vector2Int> spawned = new List<Vector2Int>();
        foreach (TreasureCollectible t in activeTreasures)
        {
            if (t == null) continue;
            Vector3 pos = t.transform.position;
            spawned.Add(new Vector2Int(
                Mathf.RoundToInt(pos.x),
                Mathf.RoundToInt(pos.z)
            ));
        }
        return spawned;
    }

    //  // Called by TrapManager when Memory Flash is used (costs flashCost fragments)
    public void ConsumeFlashCharge(int cost)
    {
        collectedCount = Mathf.Max(0, collectedCount - cost);
        flashValue = Mathf.Clamp01(flashValue - flashIncreasePerTreasure * cost);
        PushHud();
    }
}
