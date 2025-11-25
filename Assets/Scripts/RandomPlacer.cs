using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RandomTownPlacer : MonoBehaviour
{
    [Header("What to spawn")]
    public List<GameObject> buildingPrefabs = new();
    public int buildingCount = 15;

    [Header("Placement area (camera)")]
    public Camera targetCamera;
    public float borderPadding = 0.5f;
    public float extraSeparation = 0.2f;

    [Header("Building colors (palette)")]
    public List<Color> buildingColors = new()
    {
        Color.white,
        new Color(0.8f, 0.8f, 0.8f),
        new Color(0.6f, 0.6f, 0.6f)
    };
    public bool colorAllChildSprites = true; // if prefabs have multiple sprites

    [Header("Randomness")]
    public bool useSeed = false;
    public int seed = 12345;

    [Header("Safety")]
    public int maxAttemptsPerBuilding = 500;

    Dictionary<GameObject, Vector2> prefabSizes = new();

    struct Rect2D { public Vector2 center, half; }
    List<Rect2D> placed = new();

    List<GameObject> spawnedBuildings = new();

    [Header("Debug")]
    public bool isDebugMode = false;

    IEnumerator Start()
    {
        // Wait until NetworkManager exists and local client is ready
        while (NetworkManager.Singleton == null ||
               !NetworkManager.Singleton.IsListening ||
               NetworkManager.Singleton.LocalClient == null)
        {
            yield return null;
        }

        // Only Host (Player1) should generate the town
        if (!NetworkManager.Singleton.IsHost)
        {
            gameObject.SetActive(false);
            yield break;
        }

        // ---- your old Start() body goes here ----
        if (useSeed) Random.InitState(seed);

        if (targetCamera == null)
            targetCamera = Camera.main;

        CachePrefabSizes();
        PlaceTown();


        if (useSeed) Random.InitState(seed);

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogError("RandomTownPlacer: Assign a camera.");
            yield break;
        }

        if (buildingPrefabs.Count == 0)
        {
            Debug.LogError("RandomTownPlacer: Add building prefabs.");
            yield break;
        }

        CachePrefabSizes();
        PlaceTown();
    }

    void CachePrefabSizes()
    {
        prefabSizes.Clear();

        foreach (var prefab in buildingPrefabs)
        {
            if (prefab == null) continue;

            var ghost = Instantiate(prefab);
            ghost.hideFlags = HideFlags.HideAndDontSave;

            Vector2 size = MeasureInstanceSize(ghost);

            Destroy(ghost);

            prefabSizes[prefab] = size;
        }
    }

    Vector2 MeasureInstanceSize(GameObject inst)
    {
        var col = inst.GetComponentInChildren<Collider2D>();
        if (col != null)
            return col.bounds.size;

        var sr = inst.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            return sr.bounds.size;

        return Vector2.one;
    }

    void PlaceTown()
    {
        // Only Host (Player1) should generate the town.
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
            return; // networking not ready yet

        if (!nm.IsHost)
            return; // P2 does nothing, but we don't kill the object

        placed.Clear();

        // keep track of spawned buildings for clue picking
        List<GameObject> spawned = new List<GameObject>(buildingCount);

        for (int i = 0; i < buildingCount; i++)
        {
            var prefab = buildingPrefabs[Random.Range(0, buildingPrefabs.Count)];
            if (prefab == null) { i--; continue; }

            Vector2 size = prefabSizes[prefab];
            Vector2 half = size * 0.5f;

            bool placedOk = false;

            for (int attempt = 0; attempt < maxAttemptsPerBuilding; attempt++)
            {
                Vector2 pos = RandomPointInsideCamera(targetCamera, half);

                if (!IntersectsPlaced(pos, half))
                {
                    var go = Instantiate(prefab, pos, Quaternion.identity, transform);
                    spawnedBuildings.Add(go);

                    ApplyRandomColor(go);

                    var marker = go.GetComponent<BuildingMarker>();
                    if (marker == null) marker = go.AddComponent<BuildingMarker>();
                    marker.isClue = false;

                    spawned.Add(go);

                    placed.Add(new Rect2D
                    {
                        center = pos,
                        half = half + Vector2.one * extraSeparation
                    });

                    placedOk = true;
                    break;
                }
            }

            if (!placedOk)
                Debug.LogWarning($"Failed to place building #{i}. Try fewer buildings or smaller prefabs.");
        }

        if (spawned.Count > 0)
        {
            int idx = Random.Range(0, spawned.Count);
            var clueGo = spawned[idx];

            var clueMarker = clueGo.GetComponent<BuildingMarker>();
            clueMarker.isClue = true;

            if (isDebugMode)
            {
                var sr = clueGo.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = Color.red;
            }

            var reporter = FindFirstObjectByType<PuzzleBuildingReporter>();
            if (reporter != null)
                reporter.ReportClueNow();
        }
    }


    public void ClearTown()
    {
        foreach (var b in spawnedBuildings)
            if (b != null) Destroy(b);

        spawnedBuildings.Clear();
    }

    public void RebuildTown(int seed)
    {
        // optional deterministic rebuild per round
        Random.InitState(seed);

        ClearTown();
        PlaceTown();

        // after placing, re-report clue to server
        var reporter = FindFirstObjectByType<PuzzleBuildingReporter>();
        if (reporter != null)
            reporter.Invoke("ReportBuildingsToServer", 0.2f);
    }

    void ApplyRandomColor(GameObject go)
    {
        if (buildingColors == null || buildingColors.Count == 0) return;

        Color c = buildingColors[Random.Range(0, buildingColors.Count)];

        if (colorAllChildSprites)
        {
            var sprites = go.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in sprites)
                sr.color = c;
        }
        else
        {
            var sr = go.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = c;
        }
    }

    Vector2 RandomPointInsideCamera(Camera cam, Vector2 halfSize)
    {
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector3 camPos = cam.transform.position;

        float minX = camPos.x - width / 2f + borderPadding + halfSize.x;
        float maxX = camPos.x + width / 2f - borderPadding - halfSize.x;
        float minY = camPos.y - height / 2f + borderPadding + halfSize.y;
        float maxY = camPos.y + height / 2f - borderPadding - halfSize.y;

        return new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY)
        );
    }

    bool IntersectsPlaced(Vector2 center, Vector2 half)
    {
        foreach (var r in placed)
        {
            if (Mathf.Abs(center.x - r.center.x) < (half.x + r.half.x) &&
                Mathf.Abs(center.y - r.center.y) < (half.y + r.half.y))
                return true;
        }
        return false;
    }
}
