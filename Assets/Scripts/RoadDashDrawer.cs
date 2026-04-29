using UnityEngine;

public class RoadDashDrawer : MonoBehaviour
{
    public Material DashMaterial; // Assign in Inspector
    public float dashLength = 0.3f;
    public float gapLength = 0.3f;
    public float lineWidth = 0.05f;
    public float yOffset = 0.05f; // Increase from 0.02 to 0.05 to lift it above the road
    void Start()
    {
        Invoke(nameof(DrawAllDashes), 0.1f);
    }

    void DrawAllDashes()
    {
        // 🔴 Make sure GridManager exists
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager Instance not found! Make sure it exists in scene.");
            return;
        }

        GridManager gm = GridManager.Instance;

        // 🔴 Safety check
        if (gm.mapLayout == null)
        {
            Debug.LogError("mapLayout is NULL in GridManager!");
            return;
        }

        int rows = gm.mapLayout.GetLength(0);
        int cols = gm.mapLayout.GetLength(1);

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < cols; x++)
            {
                if (gm.mapLayout[z, x] != 1) continue;

                Vector3 center = gm.GridToWorld(x, z);
                center.y += yOffset;

                bool leftRoad = (x > 0) && gm.mapLayout[z, x - 1] == 1;
                bool rightRoad = (x < cols - 1) && gm.mapLayout[z, x + 1] == 1;
                bool downRoad = (z < rows - 1) && gm.mapLayout[z + 1, x] == 1;
                bool upRoad = (z > 0) && gm.mapLayout[z - 1, x] == 1;

                // Horizontal dash
                if (leftRoad || rightRoad)
                {
                    DrawDashLine(
                        center + new Vector3(-0.5f, 0, 0),
                        center + new Vector3(0.5f, 0, 0)
                    );
                }

                // Vertical dash
                if (upRoad || downRoad)
                {
                    DrawDashLine(
                        center + new Vector3(0, 0, -0.5f),
                        center + new Vector3(0, 0, 0.5f)
                    );
                }
            }
        }
    }

    void DrawDashLine(Vector3 start, Vector3 end)
    {
        float totalLen = Vector3.Distance(start, end);
        Vector3 dir = (end - start).normalized;

        float traveled = 0f;
        bool draw = true;

        while (traveled < totalLen)
        {
            float segLen = Mathf.Min(draw ? dashLength : gapLength, totalLen - traveled);

            if (draw)
            {
                Vector3 a = start + dir * traveled;
                Vector3 b = start + dir * (traveled + segLen);
                CreateDash(a, b);
            }

            traveled += segLen;
            draw = !draw;
        }
    }

    void CreateDash(Vector3 a, Vector3 b)
    {
        GameObject go = new GameObject("Dash");
        go.transform.SetParent(transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();

        // ✅ FIXED (correct variable name)
        lr.material = DashMaterial;

        // 🔴 fallback if no material assigned
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = Color.white;

        lr.material = mat;
        lr.startColor = Color.white;
        lr.endColor = Color.white;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);

        lr.useWorldSpace = true;

        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }
}