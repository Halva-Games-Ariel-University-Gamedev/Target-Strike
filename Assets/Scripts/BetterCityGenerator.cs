using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BetterCityGenerator : MonoBehaviour
{
    [Header("General")]
    public GameObject buildingPrefab;
    public GameObject smallBuildingPrefab;

    [Tooltip("If true, a new random seed is chosen every play.")]
    public bool randomSeedOnPlay = true;

    [Tooltip("If randomSeedOnPlay is false, this seed is used.")]
    public int seed = 12345;

    [Header("Layout (10 buildings = 5 x 2 grid)")]
    public int buildingsX = 5;
    public int buildingsZ = 2;
    public float spacing = 15f; // distance between centers

    [Header("Building size ranges")]
    public Vector2 sizeXRange = new Vector2(6f, 12f);
    public Vector2 sizeZRange = new Vector2(6f, 12f);
    public Vector2 heightRange = new Vector2(15f, 40f);

    [Header("Mission data")]
    public TMP_Text missionHintBox = null;

    private readonly List<GameObject> _spawned = new();

    private bool betterWorldGen;

    void Start()
    {
        var mission = MissionsMenu.Instance.CurrentMission;

        if (mission != null)
        {
            buildingsX = mission.buildingsX;
            buildingsZ = mission.buildingsZ;
            spacing = mission.spacing;
            seed = mission.seed;
            sizeXRange = mission.sizeXRange;
            sizeZRange = mission.sizeZRange;
            heightRange = mission.heightRange;
            betterWorldGen = mission.worldGen == WorldGenType.BETTER;
        }

        if (betterWorldGen) Generate(seed, mission);
        else BasicGenerate(seed, mission);

        if (mission != null && mission.spawnCars) GetComponent<ParkedCarSpawner>().SpawnParkedCars(seed);
    }

    void BasicGenerate(int seed, MissionConfig mission)
    {
        Random.InitState(seed);

        float offsetX = -(buildingsX - 1) * spacing * 0.5f;
        float offsetZ = -(buildingsZ - 1) * spacing * 0.5f;

        for (int gx = 0; gx < buildingsX; gx++)
        {
            for (int gz = 0; gz < buildingsZ; gz++)
            {
                bool isTarget = mission != null &&
                                mission.target != null &&
                                gx == mission.target.x &&
                                gz == mission.target.z;

                // size
                float sizeX = isTarget && mission.target.overrideSize
                    ? mission.target.widthX
                    : Random.Range(sizeXRange.x, sizeXRange.y);

                float sizeZ = isTarget && mission.target.overrideSize
                    ? mission.target.depthZ
                    : Random.Range(sizeZRange.x, sizeZRange.y);

                float height = isTarget && mission.target.overrideSize
                    ? mission.target.height
                    : Random.Range(heightRange.x, heightRange.y);

                float fw = Mathf.Max(0.01f, mission.floorWidth);
                float fh = Mathf.Max(0.01f, mission.floorHeight);

                // snap width/depth to multiples of fw
                int segX = Mathf.Max(1, Mathf.RoundToInt(sizeX / fw));
                int segZ = Mathf.Max(1, Mathf.RoundToInt(sizeZ / fw));
                sizeX = segX * fw;
                sizeZ = segZ * fw;

                // snap height to multiples of fh
                int floors = Mathf.Max(1, Mathf.RoundToInt(height / fh));
                height = floors * fh;

                float posX = offsetX + gx * spacing;
                float posZ = offsetZ + gz * spacing;

                Vector3 pos = new Vector3(posX, 0f, posZ);
                var building = Instantiate(buildingPrefab, pos, Quaternion.identity, transform);

                var info = building.GetComponent<BuildingInfo>();
                if (!info) info = building.AddComponent<BuildingInfo>();

                info.gridX = gx;
                info.gridZ = gz;
                info.id = gz * buildingsX + gx;
                info.isTarget = isTarget;

                int buildingSeed = seed ^ (info.id * 73856093) ^ (gx * 19349663) ^ (gz * 83492791);
                info.Configure(sizeX, height, sizeZ, buildingSeed, fw, fh);
            }
        }

        if (missionHintBox != null && mission != null && mission.hints != null)
        {
            missionHintBox.text = string.Join("\n", mission.hints.Select(e => e.text).ToArray());
        }
    }


    /// <summary>
    /// Generate the buildings with a specific seed.
    /// Call this from host + client later for the same layout.
    /// </summary>
    public void BasicGenerate(int newSeed)
    {
        seed = newSeed;
        Clear();

        Random.InitState(seed);

        int id = 0;

        // Center the grid around (0,0)
        float offsetX = -(buildingsX - 1) * spacing * 0.5f;
        float offsetZ = -(buildingsZ - 1) * spacing * 0.5f;

        for (int x = 0; x < buildingsX; x++)
        {
            for (int z = 0; z < buildingsZ; z++)
            {
                // Choose a random size
                float sizeX = Random.Range(sizeXRange.x, sizeXRange.y);
                float sizeZ = Random.Range(sizeZRange.x, sizeZRange.y);
                float height = Random.Range(heightRange.x, heightRange.y);

                // Grid position + small random offset so it feels more organic
                float posX = offsetX + x * spacing + Random.Range(-2f, 2f);
                float posZ = offsetZ + z * spacing + Random.Range(-2f, 2f);

                Vector3 position = new Vector3(posX, height * 0.5f, posZ);
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                GameObject building = Instantiate(buildingPrefab, position, rotation, transform);

                building.transform.localScale = new Vector3(sizeX, height, sizeZ);

                // Assign ID + store
                var info = building.GetComponent<BuildingInfo>();
                if (info == null) info = building.AddComponent<BuildingInfo>();
                info.id = id;
                id++;

                _spawned.Add(building);
            }
        }
    }


    const float BaseSmallChance = 0.20f;

    const float NeighborBonus = 0.20f;

    public void Generate(int newSeed, MissionConfig mission = null)
    {
        seed = newSeed;
        Clear();

        // We'll do a deterministic two-pass:
        // 1) Decide which grid cells are "small" using neighbor-boosted probability (based on already-decided neighbors).
        // 2) Instantiate prefabs accordingly.

        Random.InitState(seed);

        // Center the grid around (0,0)
        float offsetX = -(buildingsX - 1) * spacing * 0.5f;
        float offsetZ = -(buildingsZ - 1) * spacing * 0.5f;

        // Pass 1: decide small/normal for each cell
        bool[,] isSmall = new bool[buildingsX, buildingsZ];

        for (int gx = 0; gx < buildingsX; gx++)
        {
            for (int gz = 0; gz < buildingsZ; gz++)
            {
                int smallNeighbors = 0;

                // Only count neighbors that are already decided to keep results stable/deterministic
                // (left and bottom if you iterate x then z; here we iterate x outer, z inner => "bottom" is gz-1, "left" is gx-1)
                if (gx > 0 && isSmall[gx - 1, gz]) smallNeighbors++;
                if (gz > 0 && isSmall[gx, gz - 1]) smallNeighbors++;

                float chance = BaseSmallChance + smallNeighbors * NeighborBonus;
                chance = Mathf.Clamp01(chance);

                isSmall[gx, gz] = Random.value < chance;
            }
        }

        // Pass 2: instantiate
        int id = 0;

        for (int gx = 0; gx < buildingsX; gx++)
        {
            for (int gz = 0; gz < buildingsZ; gz++)
            {
                bool isTarget =
                    mission != null &&
                    mission.target != null &&
                    gx == mission.target.x &&
                    gz == mission.target.z;

                // Pick prefab (keep target as normal building unless you want it eligible too)
                GameObject prefabToUse = isSmall[gx, gz] ? smallBuildingPrefab : buildingPrefab;
                if (prefabToUse == null) prefabToUse = buildingPrefab; // safety fallback

                // size / height (same logic as your mission-based snapping)
                float sizeX = isTarget && mission.target.overrideSize
                    ? mission.target.widthX
                    : Random.Range(sizeXRange.x, sizeXRange.y);

                float sizeZ = isTarget && mission.target.overrideSize
                    ? mission.target.depthZ
                    : Random.Range(sizeZRange.x, sizeZRange.y);

                float height = isTarget && mission.target.overrideSize
                    ? mission.target.height
                    : Random.Range(heightRange.x, heightRange.y);

                if (isSmall[gx, gz]) height = Random.Range(1, 3) * 5;

                float fw = Mathf.Max(0.01f, mission != null ? mission.floorWidth : 1f);
                float fh = Mathf.Max(0.01f, mission != null ? mission.floorHeight : 1f);

                // snap width/depth to multiples of fw
                int segX = Mathf.Max(1, Mathf.RoundToInt(sizeX / fw));
                int segZ = Mathf.Max(1, Mathf.RoundToInt(sizeZ / fw));
                sizeX = segX * fw;
                sizeZ = segZ * fw;

                // snap height to multiples of fh
                int floors = Mathf.Max(1, Mathf.RoundToInt(height / fh));
                height = floors * fh;

                float posX = offsetX + gx * spacing;
                float posZ = offsetZ + gz * spacing;

                Vector3 pos = new Vector3(posX, 0f, posZ);
                var building = Instantiate(prefabToUse, pos, Quaternion.identity, transform);

                var info = building.GetComponent<BuildingInfo>();
                if (!info) info = building.AddComponent<BuildingInfo>();

                info.gridX = gx;
                info.gridZ = gz;
                info.id = gz * buildingsX + gx;   // keep your existing id scheme
                info.isTarget = isTarget;

                int buildingSeed = seed ^ (info.id * 73856093) ^ (gx * 19349663) ^ (gz * 83492791);
                info.Configure(sizeX, height, sizeZ, buildingSeed, fw, fh);

                _spawned.Add(building);
                id++;
            }
        }

        if (missionHintBox != null && mission != null && mission.hints != null)
            missionHintBox.text = string.Join("\n", mission.hints.Select(e => e.text).ToArray());
    }


    /// <summary>
    /// Generate the buildings with a specific seed.
    /// Call this from host + client later for the same layout.
    /// </summary>
    public void Generate(int newSeed)
    {
        seed = newSeed;
        Clear();

        Random.InitState(seed);

        int id = 0;

        // Center the grid around (0,0)
        float offsetX = -(buildingsX - 1) * spacing * 0.5f;
        float offsetZ = -(buildingsZ - 1) * spacing * 0.5f;

        for (int x = 0; x < buildingsX; x++)
        {
            for (int z = 0; z < buildingsZ; z++)
            {
                // Choose a random size
                float sizeX = Random.Range(sizeXRange.x, sizeXRange.y);
                float sizeZ = Random.Range(sizeZRange.x, sizeZRange.y);
                float height = Random.Range(heightRange.x, heightRange.y);

                // Grid position + small random offset so it feels more organic
                float posX = offsetX + x * spacing + Random.Range(-2f, 2f);
                float posZ = offsetZ + z * spacing + Random.Range(-2f, 2f);

                Vector3 position = new Vector3(posX, height * 0.5f, posZ);
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                GameObject building = Instantiate(buildingPrefab, position, rotation, transform);

                building.transform.localScale = new Vector3(sizeX, height, sizeZ);

                // Assign ID + store
                var info = building.GetComponent<BuildingInfo>();
                if (info == null) info = building.AddComponent<BuildingInfo>();
                info.id = id;
                id++;

                _spawned.Add(building);
            }
        }
    }

    public void Clear()
    {
        foreach (var go in _spawned)
        {
            if (go != null)
                Destroy(go);
        }

        _spawned.Clear();
    }
}
