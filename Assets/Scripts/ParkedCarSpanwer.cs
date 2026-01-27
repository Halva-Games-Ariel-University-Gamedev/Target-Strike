using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParkedCarSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject carPrefab;
    public Material overrideCarMaterial; // assign a URP Lit or Standard material (no texture)


    [Header("How many cars")]
    public int carsToSpawn = 25;

    [Header("Sampling")]
    public int maxAttemptsPerCar = 120;
    public float groundY = 0f;

    [Header("Clearances")]
    public float buildingClearance = 1.0f;
    public float carClearance = 2.0f;

    [Header("City Bounds (optional)")]
    public Transform boundsMin;
    public Transform boundsMax;

    [Header("Coloring")]
    static readonly Color[] BW_DISTINCT_COLORS =
    {
        new Color(0.10f, 0.10f, 0.45f), // dark blue   (dark)
        new Color(0.35f, 0.05f, 0.05f), // dark red    (dark-ish, but different hue if you ever turn color on)
        new Color(0.10f, 0.45f, 0.10f), // dark green  (mid-dark)
        new Color(0.55f, 0.40f, 0.05f), // mustard     (mid)
        new Color(0.55f, 0.15f, 0.65f), // purple      (mid-bright)
        new Color(0.85f, 0.85f, 0.20f), // yellow      (bright)
        new Color(0.70f, 0.85f, 0.95f), // light cyan  (very bright)
        new Color(0.92f, 0.92f, 0.92f), // near white  (max)
    };

    // Optional: names you store in CarInfo alongside the color.
    // Keep same ordering as BW_DISTINCT_COLORS.
    static readonly string[] BW_DISTINCT_COLOR_NAMES =
    {
        "Dark Blue",
        "Dark Red",
        "Dark Green",
        "Mustard",
        "Purple",
        "Yellow",
        "Light Cyan",
        "White",
    };

    readonly List<Rect> _buildingRects = new();
    readonly List<Vector3> _spawned = new();

    // car footprint (XZ half extents)
    float _carHalfX;
    float _carHalfZ;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    public void SpawnParkedCars(int seed)
    {
        if (!carPrefab)
        {
            Debug.LogError("ParkedCarSpawner: carPrefab is null.");
            return;
        }

        BuildBuildingRects();
        if (_buildingRects.Count == 0)
        {
            Debug.LogWarning("ParkedCarSpawner: no buildings found (expects BuildingInfo under this object).");
            return;
        }

        ComputeCarFootprintXZ();

        UnityEngine.Random.InitState(seed ^ 0x51C0FFEE);

        Rect city = GetCityBoundsXZ();

        int spawnedCount = 0;
        for (int i = 0; i < carsToSpawn; i++)
        {
            bool placed = false;

            for (int attempt = 0; attempt < maxAttemptsPerCar; attempt++)
            {
                float x = UnityEngine.Random.Range(city.xMin, city.xMax);
                float z = UnityEngine.Random.Range(city.yMin, city.yMax);
                Vector3 pos = new Vector3(x, groundY, z);

                if (!IsFree(pos))
                    continue;

                Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

                var car = Instantiate(carPrefab, pos, rot, transform);
                _spawned.Add(pos);
                ApplyRandomColor(car);

                int idx = UnityEngine.Random.Range(0, BW_DISTINCT_COLORS.Length);
                Color color = BW_DISTINCT_COLORS[idx];
                ConfigureSpawnedCar(car, id: spawnedCount, color: color, idx);

                spawnedCount++;
                placed = true;
                break;
            }

            if (!placed) break;
        }

        Debug.Log($"ParkedCarSpawner: spawned {spawnedCount}/{carsToSpawn} cars.");
    }

    void ConfigureSpawnedCar(GameObject car, int id, Color color, int colorindex)
    {
        // Put the car on the Car layer (make sure you created it in Unity)
        int carLayer = LayerMask.NameToLayer("Car");
        if (carLayer != -1)
        {
            foreach (Transform t in car.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = carLayer;
        }

        // Ensure it has a collider so your raycast can hit it
        if (!car.GetComponentInChildren<Collider>())
        {
            Bounds b = GetWorldBoundsFromRenderers(car);
            var bc = car.AddComponent<BoxCollider>();

            // Convert world bounds to local collider params
            bc.center = car.transform.InverseTransformPoint(b.center);

            Vector3 lossy = car.transform.lossyScale;
            bc.size = new Vector3(
                Mathf.Abs(lossy.x) < 1e-6f ? b.size.x : b.size.x / Mathf.Abs(lossy.x),
                Mathf.Abs(lossy.y) < 1e-6f ? b.size.y : b.size.y / Mathf.Abs(lossy.y),
                Mathf.Abs(lossy.z) < 1e-6f ? b.size.z : b.size.z / Mathf.Abs(lossy.z)
            );
        }

        // Init CarInfo (sets id + renderers + base color + supports highlight)
        var rs = car.GetComponentsInChildren<Renderer>();
        var info = car.GetComponent<CarInfo>();
        if (!info) info = car.AddComponent<CarInfo>();
        string name = BW_DISTINCT_COLOR_NAMES[colorindex];

        info.Init(newId: _spawned.Count, colorName: name, baseColor: color, rs: car.GetComponentsInChildren<Renderer>());

    }

    Bounds GetWorldBoundsFromRenderers(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);

        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++)
            b.Encapsulate(rs[i].bounds);

        return b;
    }

    void BuildBuildingRects()
    {
        _buildingRects.Clear();
        _spawned.Clear();

        // Your buildings are the direct spawned children
        // and you scale the ROOT to (sizeX, height, sizeZ)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform t = transform.GetChild(i);

            // Only treat actual buildings as obstacles
            // (skip parked cars or other stuff you parent here)
            if (!t.TryGetComponent<BuildingInfo>(out _))
                continue;

            Vector3 p = t.position;
            Vector3 s = t.lossyScale; // world size (x,z are footprint)

            // footprint rect in XZ
            Rect rect = Rect.MinMaxRect(
                p.x - s.x * 0.5f,
                p.z - s.z * 0.5f,
                p.x + s.x * 0.5f,
                p.z + s.z * 0.5f
            );

            rect = Inflate(rect, buildingClearance);
            _buildingRects.Add(rect);
        }
    }

    void ComputeCarFootprintXZ()
    {
        // Instantiate temporarily to read renderer bounds reliably
        var temp = Instantiate(carPrefab);
        temp.hideFlags = HideFlags.HideAndDontSave;

        // ensure at origin (bounds are world-space)
        temp.transform.position = Vector3.zero;
        temp.transform.rotation = Quaternion.identity;
        temp.transform.localScale = Vector3.one;

        var renderers = temp.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            _carHalfX = 0.6f;
            _carHalfZ = 1.2f;
        }
        else
        {
            Bounds bb = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bb.Encapsulate(renderers[i].bounds);

            _carHalfX = bb.extents.x;
            _carHalfZ = bb.extents.z;
        }

        DestroyImmediate(temp);

        // add a little margin so it doesn't graze buildings
        _carHalfX += 0.1f;
        _carHalfZ += 0.1f;
    }

    Rect GetCityBoundsXZ()
    {
        if (boundsMin && boundsMax)
        {
            Vector3 a = boundsMin.position;
            Vector3 b = boundsMax.position;
            return Rect.MinMaxRect(
                Mathf.Min(a.x, b.x), Mathf.Min(a.z, b.z),
                Mathf.Max(a.x, b.x), Mathf.Max(a.z, b.z)
            );
        }

        float minX = _buildingRects.Min(r => r.xMin);
        float maxX = _buildingRects.Max(r => r.xMax);
        float minZ = _buildingRects.Min(r => r.yMin);
        float maxZ = _buildingRects.Max(r => r.yMax);

        // also shrink a bit by car extents so we don't spawn half outside
        return Rect.MinMaxRect(minX + _carHalfX, minZ + _carHalfZ, maxX - _carHalfX, maxZ - _carHalfZ);
    }

    bool IsFree(Vector3 p)
    {
        // car footprint at this point (XZ rect)
        Rect carRect = Rect.MinMaxRect(
            p.x - _carHalfX, p.z - _carHalfZ,
            p.x + _carHalfX, p.z + _carHalfZ
        );

        // must not overlap any building rect
        for (int i = 0; i < _buildingRects.Count; i++)
        {
            if (carRect.Overlaps(_buildingRects[i]))
                return false;
        }

        // must not be too close to other cars (center distance)
        float minD2 = carClearance * carClearance;
        for (int i = 0; i < _spawned.Count; i++)
        {
            if ((p - _spawned[i]).sqrMagnitude < minD2)
                return false;
        }

        return true;
    }

    static Rect Inflate(Rect r, float m)
        => new Rect(r.xMin - m, r.yMin - m, r.width + 2f * m, r.height + 2f * m);

    void ApplyRandomColor(GameObject car)
    {
        Color c = BW_DISTINCT_COLORS[UnityEngine.Random.Range(0, BW_DISTINCT_COLORS.Length)];
        var block = new MaterialPropertyBlock();

        foreach (var r in car.GetComponentsInChildren<Renderer>())
        {
            // Same material asset for all cars (fine)
            if (overrideCarMaterial != null)
                r.sharedMaterial = overrideCarMaterial;

            // Different color per car (this is the important part)
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }

}
