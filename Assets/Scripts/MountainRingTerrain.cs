using UnityEngine;

public class MountainRingTerrain : MonoBehaviour
{
    [Header("Center (town)")]
    public Transform center;              // town center (X,Z used)
    public float townRadius = 120f;       // flat/safe area radius

    [Header("Mountain ring distances")]
    public float mountainStart = 160f;    // where mountains begin (from center)
    public float mountainPeak = 260f;     // where they reach max
    public float mountainEnd = 420f;      // fade out after this

    [Header("Terrain size")]
    public Vector3 terrainSize = new Vector3(1000f, 200f, 1000f);
    [Range(129, 2049)] public int heightmapResolution = 513;

    [Header("Heights")]
    [Range(0f, 1f)] public float townBaseHeight01 = 0.02f; // normalized (0..1 of terrain height)
    public float mountainMaxHeight = 80f;                  // meters (world units)

    [Header("Noise")]
    public int seed = 12345;
    public float noiseScale = 0.006f;   // smaller = bigger features
    [Range(1, 8)] public int octaves = 5;
    [Range(0.1f, 0.9f)] public float persistence = 0.5f;
    [Range(1.5f, 4f)] public float lacunarity = 2f;
    public bool ridged = true;         // sharper mountains

    [Header("Generate")]
    public bool autoRegenerateInEditor = false;

    Terrain terrain;

    void OnEnable()
    {
        terrain = GetComponent<Terrain>();
        EnsureTerrainData();
        if (autoRegenerateInEditor) Generate();
    }

    void OnValidate()
    {
        if (!enabled) return;
        terrain = GetComponent<Terrain>();
        EnsureTerrainData();
        if (autoRegenerateInEditor) Generate();
    }

    void EnsureTerrainData()
    {
        if (terrain.terrainData == null)
            terrain.terrainData = new TerrainData();

        var td = terrain.terrainData;

        // Force valid heightmap resolution (Unity wants 2^n + 1)
        heightmapResolution = ClosestPow2Plus1(heightmapResolution);
        td.heightmapResolution = heightmapResolution;
        td.size = terrainSize;
    }

    [ContextMenu("Generate Mountains Ring")]
    public void Generate()
    {
        if (terrain == null) terrain = GetComponent<Terrain>();
        var td = terrain.terrainData;
        if (td == null) return;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 c = center ? center.position : (terrainPos + new Vector3(td.size.x * 0.5f, 0f, td.size.z * 0.5f));

        int res = td.heightmapResolution;
        float[,] heights = new float[res, res];

        var prng = new System.Random(seed);
        float ox = (float)prng.NextDouble() * 10000f;
        float oz = (float)prng.NextDouble() * 10000f;

        float maxH01 = mountainMaxHeight / Mathf.Max(0.01f, td.size.y);

        for (int z = 0; z < res; z++)
        {
            float tz = (float)z / (res - 1);
            float worldZ = terrainPos.z + tz * td.size.z;

            for (int x = 0; x < res; x++)
            {
                float tx = (float)x / (res - 1);
                float worldX = terrainPos.x + tx * td.size.x;

                float dist = Vector2.Distance(new Vector2(worldX, worldZ), new Vector2(c.x, c.z));

                // Ring mask: 0 inside townRadius, ramps up to peak, then ramps down to 0
                float mask = RingMask(dist, townRadius, mountainStart, mountainPeak, mountainEnd);

                // Noise for mountains
                float n = FBm(worldX, worldZ, ox, oz);
                if (ridged) n = 1f - Mathf.Abs(2f * n - 1f); // ridged transform

                float h = townBaseHeight01 + mask * n * maxH01;
                heights[z, x] = Mathf.Clamp01(h);
            }
        }

        td.SetHeights(0, 0, heights);
    }

    float RingMask(float dist, float flatR, float start, float peak, float end)
    {
        if (dist <= flatR) return 0f;
        if (dist <= start) return 0f;

        float up = Mathf.InverseLerp(start, peak, dist);
        float down = 1f - Mathf.InverseLerp(peak, end, dist);

        up = Smooth01(up);
        down = Smooth01(down);

        return Mathf.Clamp01(up * down);
    }

    float Smooth01(float t) => t * t * (3f - 2f * t); // SmoothStep 0..1

    float FBm(float worldX, float worldZ, float ox, float oz)
    {
        float amp = 1f;
        float freq = 1f;
        float sum = 0f;
        float norm = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float nx = (worldX + ox) * noiseScale * freq;
            float nz = (worldZ + oz) * noiseScale * freq;

            float p = Mathf.PerlinNoise(nx, nz); // 0..1
            sum += p * amp;
            norm += amp;

            amp *= persistence;
            freq *= lacunarity;
        }

        return (norm > 0f) ? (sum / norm) : 0f;
    }

    int ClosestPow2Plus1(int v)
    {
        // Make it 2^n + 1
        v = Mathf.Clamp(v, 129, 2049);
        int p = 1;
        while (p < v - 1) p <<= 1;
        int a = p + 1;
        int b = (p >> 1) + 1;
        return (Mathf.Abs(a - v) < Mathf.Abs(b - v)) ? a : b;
    }
}
