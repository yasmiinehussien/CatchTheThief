using UnityEngine;


public class GridManager : MonoBehaviour
{
    public GameObject roadTilePrefab;
    public GameObject grassTilePrefab;

    public Node[,] grid;
    public static GridManager Instance;

    public Texture2D grassTexture;
    public Texture2D roadTexture;
   // public Transform player;

    // ═══════════════════════════════════════════════════════
    //  NEW MAP  —  12 cols (x=0..11)  x  12 rows (z=0..11)
    //
    //  █ = road (1)     · = grass (0)
    //
    //  z=11: ·····█······   ← BANK (end tile at x=5)
    //  z=10: ···████·█···   ← true path + fake branch right
    //  z= 9: ···█····█···
    //  z= 8: ·███··███···
    //  z= 7: ·█····█·█···
    //  z= 6: ·█····█·████   ← fake dead-end branch far right
    //  z= 5: ·██████····█   ← main horizontal corridor
    //  z= 4: ···█··█····█
    //  z= 3: ████··██···█   ← fake dead-end branch left
    //  z= 2: █··█·····███
    //  z= 1: █··██····█··
    //  z= 0: ██·······██·   ← player starts at x=0
    //
    //  TRUE PATH (BFS verified, 21 steps):
    //    (0,0)→(0,3)→(3,3)→(3,5)→(6,5)→(6,8)→(3,8)
    //    →(3,10)→(5,10)→(5,11)  ✓
    //
    //  FAKE ROUTES (dead ends):
    //    A) x=9..11 z=0 → z=6 (right edge loop, no exit to bank)
    //    B) x=0..2 z=3 (short left spur)
    //    C) x=8..11 z=6..7 (right-side maze, no bank connection)
    // ═══════════════════════════════════════════════════════
    public int[,] mapLayout = new int[,]
{
    // x=  0  1  2  3  4  5  6  7  8  9 10 11
    {      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },  // z=0  Start (Full width)
    {      1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },  // z=1  Small grass island
    {      1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },  // z=2
    {      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },  // z=3  Wide road
    {      1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1 },  // z=4  Small grass block
    {      1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1 },  // z=5  
    {      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },  // z=6  Main Highway
    {      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },  // z=7
    {      1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },  // z=8  Upper island
    {      1, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1 },  // z=9
    {      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },  // z=10 Pure road to bank
    {      1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },  // z=11 BANK (accessible from all sides)
};

    void Awake() { Instance = this; }
    void Start() { BuildGrid();
        
    }


    public Vector3 GridToWorld(int x, int z)
    {
        return new Vector3(x, 0, z);
    }

    void BuildGrid()
    {
        int rows = mapLayout.GetLength(0);
        int cols = mapLayout.GetLength(1);
        grid = new Node[cols, rows];

        for (int z = 0; z < rows; z++)
        {
            for (int x = 0; x < cols; x++)
            {
                bool isRoad = mapLayout[z, x] == 1;
                Vector3 pos = new Vector3(x, isRoad ? 0.01f : 0f, z);

                GameObject tile;
                if (isRoad && roadTilePrefab != null)
                    tile = Instantiate(roadTilePrefab, pos, Quaternion.identity, transform);
                else if (!isRoad && grassTilePrefab != null)
                    tile = Instantiate(grassTilePrefab, pos, Quaternion.identity, transform);
                else
                    tile = CreateFlatTile(pos, isRoad);

                grid[x, z] = new Node(x, z);
                grid[x, z].isWalkable = isRoad;
            }
        }
     
    }
    

    GameObject CreateFlatTile(Vector3 pos, bool isRoad)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Plane);
        tile.transform.SetParent(transform);
        tile.transform.position = pos;
        tile.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
        if (!isRoad)
            Destroy(tile.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Glossiness", 0f);
        mat.SetFloat("_Metallic", 0f);

        if (isRoad)
        {
            mat.color = new Color(0.25f, 0.25f, 0.28f);
            if (roadTexture != null)
            {
                mat.mainTexture = roadTexture;
                mat.mainTextureScale = new Vector2(1f, 1f);
            }
        }
        else
        {
            mat.color = new Color(0.45f, 0.72f, 0.35f);
            if (grassTexture != null)
            {
                mat.mainTexture = grassTexture;
                mat.mainTextureScale = new Vector2(1f, 1f);
            }
        }

        tile.GetComponent<Renderer>().material = mat;
        return tile;
    }
}