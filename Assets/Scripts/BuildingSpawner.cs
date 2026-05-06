using UnityEngine;

public class BuildingSpawner : MonoBehaviour
{
    [Header("Toon City Asset Slots")]
    public GameObject[] buildingPrefabs;
    public GameObject[] treePrefabs;
    public GameObject shopPrefab;         // Bank — End Tile
    public GameObject fragmentLightPrefab;

    void Start()
    {
        SpawnBackground();
        SpawnBuildings();
        SpawnShop();
        SpawnNature();
    }

    // ─────────────────────────────────────────────────────────
    // BACKGROUND — Layered planes to create depth
    // ─────────────────────────────────────────────────────────
    void SpawnBackground()
    {
        Vector3 center = new Vector3(5.5f, 0f, 5.5f);

        // Outer area (main grass - bright and lively)
        CreatePlane(
            "OuterGrass",
            center + Vector3.down * 0.08f,
            new Vector3(6.0f, 1f, 6.0f),
            new Color(0.6f, 0.85f, 0.5f) // vibrant soft green
        );

        // Inner area (slightly darker for depth contrast)
        CreatePlane(
            "InnerGrass",
            center + Vector3.down * 0.04f,
            new Vector3(4.0f, 1f, 4.0f),
            new Color(0.45f, 0.7f, 0.38f) // natural darker grass
        );
    }
    void CreatePlane(string name, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Plane);
        p.name = name;
        p.transform.SetParent(transform);
        p.transform.position = pos;
        p.transform.localScale = scale;

        // Tag the grass as a Wall so the player's movement script recognizes it
        p.tag = "Wall";

        Material m = new Material(Shader.Find("Standard"));
        m.color = color;
        m.SetFloat("_Glossiness", 0f);
        p.GetComponent<Renderer>().material = m;

        // INSTEAD OF Destroy(p.GetComponent<Collider>());
        // We ensure the collider is active and solid
        MeshCollider meshCol = p.GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            meshCol.isTrigger = false;
        }
    }
    // ─────────────────────────────────────────────────────────
    // BUILDINGS — Original Positions Retained
    // ─────────────────────────────────────────────────────────
    void SpawnBuildings()
    {
        (Vector3 pos, float rot)[] sides =
        {
            (new Vector3(-2.8f, 0f, 3f),  0f),
            (new Vector3(-2.8f, 0f, 8f),  0f),
            (new Vector3(13.2f, 0f, 3f), 0f),
            (new Vector3(13.2f, 0f, 8f), 0f),
        };

        Vector3[] bgPos =
        {
            new Vector3(1f,  0f, 15f),
            new Vector3(10f, 0f, 15f),
        };

        if (buildingPrefabs == null || buildingPrefabs.Length == 0) return;

        int i = 0;
        foreach (var (pos, rot) in sides)
        {
            GameObject prefab = buildingPrefabs[i++ % buildingPrefabs.Length];
            GameObject b = Instantiate(prefab, pos, Quaternion.Euler(0f, rot, 0f), transform);
            b.tag = "Wall";
            EnsureCollider(b);
        }

        foreach (var pos in bgPos)
        {
            GameObject prefab = buildingPrefabs[i++ % buildingPrefabs.Length];
            Instantiate(prefab, pos, Quaternion.Euler(0f, 180f, 0f), transform);
        }
    }

    // ─────────────────────────────────────────────────────────
    // BANK — Centered at the end of the path
    // ─────────────────────────────────────────────────────────
    void SpawnShop()
    {
        if (shopPrefab == null) return;

        Vector3 pos = new Vector3(5.5f, 0f, 12.5f);
        GameObject shop = Instantiate(shopPrefab, pos, Quaternion.Euler(0f, 180f, 0f), transform);
        shop.name = "Bank";
        shop.tag = "EndTile";
        EnsureCollider(shop);

        GameObject glow = new GameObject("BankGlow");
        glow.transform.position = pos + Vector3.up * 2f;

        Light lightComp = glow.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = new Color(1f, 0.9f, 0.5f);
        lightComp.range = 7f;
        lightComp.intensity = 4f;
        lightComp.shadows = LightShadows.Soft;
    }

    // ─────────────────────────────────────────────────────────
    // TREES — Manual placements on Grass Islands (MapLayout 0s)
    // ─────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────
    // TREES — Automatically spawns on every '0' (Grass) tile
    // ─────────────────────────────────────────────────────────
    void SpawnNature()
    {
        // Safety check to make sure the GridManager exists
        if (GridManager.Instance == null || GridManager.Instance.mapLayout == null)
        {
            Debug.LogWarning("BuildingSpawner: Cannot find GridManager.mapLayout to spawn trees!");
            return;
        }

        int rows = GridManager.Instance.mapLayout.GetLength(0);
        int cols = GridManager.Instance.mapLayout.GetLength(1);

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < cols; x++)
            {
                // Check if this specific tile is Grass (0)
                if (GridManager.Instance.mapLayout[z, x] == 0)
                {
                    // Convert the grid (x, z) to world coordinates (x, 0, z)
                    Vector3 spawnPos = new Vector3(x, 0f, z);

                    // Optional: Add a small random offset so trees aren't perfectly centered
                    spawnPos.x += Random.Range(-0.1f, 0.1f);
                    spawnPos.z += Random.Range(-0.1f, 0.1f);

                    SpawnOneTree(spawnPos);
                }
            }
        }
    }
    void SpawnOneTree(Vector3 pos)
    {
        if (treePrefabs != null && treePrefabs.Length > 0)
        {
            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            GameObject t = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            t.transform.localScale = Vector3.one * 0.45f;
            EnsureCollider(t);
        }
    }

    void EnsureCollider(GameObject go)
    {
        Collider[] existing = go.GetComponentsInChildren<Collider>(true);
        if (existing != null && existing.Length > 0)
        {
            foreach (var c in existing)
                c.isTrigger = false;
            return;
        }

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = false;
    }
}