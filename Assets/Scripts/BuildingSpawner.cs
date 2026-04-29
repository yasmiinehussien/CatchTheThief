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
        Material m = new Material(Shader.Find("Standard"));
        m.color = color;
        m.SetFloat("_Glossiness", 0f);
        p.GetComponent<Renderer>().material = m;
        Destroy(p.GetComponent<Collider>());
    }

    // ─────────────────────────────────────────────────────────
    // BUILDINGS — Placed further back to clear the Bank
    // ─────────────────────────────────────────────────────────
    void SpawnBuildings()
    {
        // Side buildings moved further out (x axis) so they don't crowd the path
        (Vector3 pos, float rot)[] sides =
        {
            (new Vector3(-2.8f, 0f, 3f),  0f),
            (new Vector3(-2.8f, 0f, 8f),  0f),
            (new Vector3(13.2f, 0f, 3f), 0f),
            (new Vector3(13.2f, 0f, 8f), 0f),
        };

        // Skyline shifted further back (z=15) so they don't overlap the Bank
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

        // Light fix: Move higher to avoid giant shadow on the road
        // 2. Add the Point Light to make it "Glow"
        GameObject glow = new GameObject("BankGlow");
        glow.transform.position = pos + Vector3.up * 2f; // Move light up so it hits the roof/sign

        Light lightComp = glow.AddComponent<Light>();
        lightComp.type = LightType.Point;
        lightComp.color = new Color(1f, 0.9f, 0.5f); // Warm yellow/gold glow
        lightComp.range = 7f;       // How far the light reaches
        lightComp.intensity = 4f; // How bright it is
        lightComp.shadows = LightShadows.Soft; // Makes it look realistic
    }

    // ─────────────────────────────────────────────────────────
    // TREES — Positioned to avoid blocking the road
    // ─────────────────────────────────────────────────────────
    void SpawnNature()
    {
        // Verified coordinates that sit in the grass corners only
        Vector3[] spots =
        {
            
            new Vector3(11f, 0f, 0f), new Vector3(15f, 0f, 3f),
            new Vector3(4f, 0f, 7f), new Vector3(7f, 0f, 7f),
            new Vector3(3f, 0f, 0f), new Vector3(12f, 0f, 10f),
            new Vector3(2f, 0f, 12f), new Vector3(9f, 0f, 12f)
        };

        foreach (var pos in spots)
            SpawnOneTree(pos);
    }

    void SpawnOneTree(Vector3 pos)
    {
        if (treePrefabs != null && treePrefabs.Length > 0)
        {
            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            GameObject t = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            // Smaller scale ensures they don't block the road view
            t.transform.localScale = Vector3.one * 0.45f;
        }
    }

    void EnsureCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() == null)
            go.AddComponent<BoxCollider>();
    }
}