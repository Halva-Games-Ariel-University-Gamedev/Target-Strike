using System.Collections.Generic;
using UnityEngine;

public class BuildingInfo : MonoBehaviour
{
    [Header("Identity")]
    public int id;
    public bool isTarget;
    public int gridX;
    public int gridZ;

    [Header("Outline")]
    public GameObject outlineMesh;

    [Header("Facade (Atlas)")]
    [SerializeField]
    private Material facadeMaterial;   // material using your 4x4 atlas texture
    [SerializeField]
    private int atlasCols = 4;
    [SerializeField]
    private int atlasRows = 4;

    [Tooltip("If your atlas is authored like spritesheets (0 is top-left), enable this.")]
    [SerializeField]
    private bool atlasIndexTopLeft = true;

    [Header("Tiles (indices 0..15 for 4x4)")]
    [SerializeField]
    private int[] windowTiles = { 0, 1, 2, 3, 4, 5, 6, 7 };
    [SerializeField]
    private int[] doorTiles = { 12, 13, 14, 15 };

    [Header("Roof Tiles (indices 0..15 for 4x4)")]
    [SerializeField]
    private int[] roofTiles = { 0, 3 };

    [Header("Floors")]
    [Min(0.01f)] public float floorHeight = 3f;

    // internal
    Transform facade;
    MeshFilter mf;
    MeshRenderer mr;
    Mesh mesh;

    void Awake()
    {
        EnsureFacade();
        SetHighlighted(false);
    }

    public void SetHighlighted(bool value)
    {
        if (outlineMesh != null)
            outlineMesh.SetActive(value);
    }

    // Call after Instantiate
    public void Configure(float width, float height, float depth, int seed, float tileW, float tileH)
    {
        EnsureFacade();

        width = Mathf.Max(0.01f, width);
        height = Mathf.Max(0.01f, height);
        depth = Mathf.Max(0.01f, depth);

        tileW = Mathf.Max(0.01f, tileW);
        tileH = Mathf.Max(0.01f, tileH);

        int floors = Mathf.Max(1, Mathf.RoundToInt(height / tileH));
        int segX = Mathf.Max(1, Mathf.RoundToInt(width / tileW));
        int segZ = Mathf.Max(1, Mathf.RoundToInt(depth / tileW));

        // (Optional safety) force exact multiples so seams align perfectly
        width = segX * tileW;
        depth = segZ * tileW;
        height = floors * tileH;

        var bc = GetComponent<BoxCollider>();
        if (bc != null)
        {
            // BoxCollider is in LOCAL space, so compensate if the parent is scaled
            var s = transform.localScale;
            float sx = Mathf.Abs(s.x) < 1e-6f ? 1f : Mathf.Abs(s.x);
            float sy = Mathf.Abs(s.y) < 1e-6f ? 1f : Mathf.Abs(s.y);
            float sz = Mathf.Abs(s.z) < 1e-6f ? 1f : Mathf.Abs(s.z);

            bc.size = new Vector3(width / sx, height / sy, depth / sz);
            bc.center = new Vector3(0f, (height * 0.5f) / sy, 0f); // bottom at y=0
        }

        if (outlineMesh != null)
        {
            var ot = outlineMesh.transform;

            // parent scale compensation (BoxCollider/localScale behavior)
            var s = transform.localScale;
            float sx = Mathf.Abs(s.x) < 1e-6f ? 1f : Mathf.Abs(s.x);
            float sy = Mathf.Abs(s.y) < 1e-6f ? 1f : Mathf.Abs(s.y);
            float sz = Mathf.Abs(s.z) < 1e-6f ? 1f : Mathf.Abs(s.z);

            const float pad = 0.1f;

            // Keep it aligned with the building (bottom on ground)
            ot.localPosition = new Vector3(0f, (height * 0.5f) / sy, 0f);

            // Slightly bigger than the building
            ot.localScale = new Vector3(
                (width + pad) / sx,
                (height + pad) / sy,
                (depth + pad) / sz
            );
        }

        BuildFacadeMesh(width, depth, floors, tileW, tileH, seed);

        facade.localPosition = Vector3.zero;
    }

    void BuildFacadeMesh(float width, float depth, int floors, float tileW, float tileH, int seed)
    {
        mesh.Clear();

        float hw = width * 0.5f;
        float hd = depth * 0.5f;

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();

        var rng = new System.Random(seed);

        // choose where the door goes on the FRONT, bottom floor
        int doorSegX = rng.Next(0, Mathf.Max(1, Mathf.RoundToInt(width / tileW)));

        int segX = Mathf.RoundToInt(width / tileW);
        int segZ = Mathf.RoundToInt(depth / tileW);

        for (int f = 0; f < floors; f++)
        {
            float y0 = f * tileH;
            float y1 = (f + 1) * tileH;

            // FRONT (+Z)
            for (int sx = 0; sx < segX; sx++)
            {
                float x0 = -hw + sx * tileW;
                float x1 = x0 + tileW;

                int tile = Pick(windowTiles, rng);
                if (f == 0 && sx == doorSegX) tile = Pick(doorTiles, rng);

                AddQuad(
                    new Vector3(x1, y0, hd),
                    new Vector3(x1, y1, hd),
                    new Vector3(x0, y1, hd),
                    new Vector3(x0, y0, hd),
                    UvForTile(tile),
                    verts, uvs, tris
                );
            }

            // BACK (-Z)
            for (int sx = 0; sx < segX; sx++)
            {
                float x0 = -hw + sx * tileW;
                float x1 = x0 + tileW;

                int tile = Pick(windowTiles, rng);

                AddQuad(
                    new Vector3(x0, y0, -hd),
                    new Vector3(x0, y1, -hd),
                    new Vector3(x1, y1, -hd),
                    new Vector3(x1, y0, -hd),
                    UvForTile(tile),
                    verts, uvs, tris
                );
            }

            // LEFT (-X)
            for (int sz = 0; sz < segZ; sz++)
            {
                float z0 = -hd + sz * tileW;
                float z1 = z0 + tileW;

                int tile = Pick(windowTiles, rng);

                AddQuad(
                    new Vector3(-hw, y0, z1),
                    new Vector3(-hw, y1, z1),
                    new Vector3(-hw, y1, z0),
                    new Vector3(-hw, y0, z0),
                    UvForTile(tile),
                    verts, uvs, tris
                );
            }

            // RIGHT (+X)
            for (int sz = 0; sz < segZ; sz++)
            {
                float z0 = -hd + sz * tileW;
                float z1 = z0 + tileW;

                int tile = Pick(windowTiles, rng);

                AddQuad(
                    new Vector3(hw, y0, z0),
                    new Vector3(hw, y1, z0),
                    new Vector3(hw, y1, z1),
                    new Vector3(hw, y0, z1),
                    UvForTile(tile),
                    verts, uvs, tris
                );
            }
        }

        // ROOF (tile it in a grid using roofTiles)
        // float yTop = floors * tileH;
        // for (int sx = 0; sx < segX; sx++)
        // {
        //     float x0 = -hw + sx * tileW;
        //     float x1 = x0 + tileW;

        //     for (int sz = 0; sz < segZ; sz++)
        //     {
        //         float z0 = -hd + sz * tileW;
        //         float z1 = z0 + tileW;

        //         int tile = Pick(roofTiles, rng);

        //         // top face, outward normal +Y
        //         AddQuad(
        //             new Vector3(x0, yTop, z0),
        //             new Vector3(x0, yTop, z1),
        //             new Vector3(x1, yTop, z1),
        //             new Vector3(x1, yTop, z0),
        //             UvForTile(tile),
        //             verts, uvs, tris
        //         );
        //     }
        // }

        // ROOF (single quad, always)
        float yTop = floors * tileH;
        int roofTile = Pick(roofTiles, rng); // uses one atlas cell

        AddQuad(
            new Vector3(-hw, yTop, -hd), // bl
            new Vector3(-hw, yTop, hd), // tl
            new Vector3(hw, yTop, hd), // tr
            new Vector3(hw, yTop, -hd), // br
            UvForTile(roofTile),
            verts, uvs, tris
        );

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    void EnsureFacade()
    {
        // Find or create Facade child
        if (facade == null)
        {
            var t = transform.Find("Facade");
            if (t != null) facade = t;
        }

        if (facade == null)
        {
            var go = new GameObject("Facade");
            go.transform.SetParent(transform, false);
            facade = go.transform;
        }

        // ALWAYS re-fetch (fixes "Missing" serialized refs)
        mf = facade.GetComponent<MeshFilter>();
        if (mf == null) mf = facade.gameObject.AddComponent<MeshFilter>();

        mr = facade.GetComponent<MeshRenderer>();
        if (mr == null) mr = facade.gameObject.AddComponent<MeshRenderer>();

        if (facadeMaterial != null)
            mr.sharedMaterial = facadeMaterial;

        if (mesh == null)
        {
            mesh = new Mesh { name = "BuildingFacadeMesh" };
            mesh.MarkDynamic();
        }

        if (mf.sharedMesh != mesh)
            mf.sharedMesh = mesh;
    }

    int Pick(int[] arr, System.Random rng)
    {
        if (arr == null || arr.Length == 0) return 0;
        return arr[rng.Next(0, arr.Length)];
    }

    Rect UvForTile(int index)
    {
        index = Mathf.Clamp(index, 0, atlasCols * atlasRows - 1);

        int x = index % atlasCols;
        int y = index / atlasCols;

        // Unity UV origin is bottom-left. Many atlases are authored top-left.
        if (atlasIndexTopLeft)
            y = (atlasRows - 1) - y;

        float du = 1f / atlasCols;
        float dv = 1f / atlasRows;

        return new Rect(x * du, y * dv, du, dv);
    }

    static void AddQuad(
        Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br,
        Rect uvRect,
        List<Vector3> verts, List<Vector2> uvs, List<int> tris)
    {
        int start = verts.Count;

        verts.Add(bl);
        verts.Add(tl);
        verts.Add(tr);
        verts.Add(br);

        uvs.Add(new Vector2(uvRect.xMin, uvRect.yMin));
        uvs.Add(new Vector2(uvRect.xMin, uvRect.yMax));
        uvs.Add(new Vector2(uvRect.xMax, uvRect.yMax));
        uvs.Add(new Vector2(uvRect.xMax, uvRect.yMin));

        tris.Add(start + 0);
        tris.Add(start + 1);
        tris.Add(start + 2);

        tris.Add(start + 0);
        tris.Add(start + 2);
        tris.Add(start + 3);
    }
}
