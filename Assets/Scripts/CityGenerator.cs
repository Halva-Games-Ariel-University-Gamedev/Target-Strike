using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class CityGenerator : MonoBehaviour
{
    [Header("General")]
    public GameObject buildingPrefab;

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
        }

        Generate(seed, mission);
    }

    void Generate(int seed, MissionConfig mission)
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

                float posX = offsetX + gx * spacing;
                float posZ = offsetZ + gz * spacing;
                Vector3 pos = new Vector3(posX, height * 0.5f, posZ);

                var building = Instantiate(buildingPrefab, pos, Quaternion.identity, transform);
                building.transform.localScale = new Vector3(sizeX, height, sizeZ);

                var info = building.GetComponent<BuildingInfo>();
                if (!info) info = building.AddComponent<BuildingInfo>();

                // id from (x,z)
                info.gridX = gx;
                info.gridZ = gz;
                info.id = gz * buildingsX + gx;
                info.isTarget = isTarget;
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
