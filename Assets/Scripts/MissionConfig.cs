using System.Collections.Generic;
using UnityEngine;

public enum WorldGenType
{
    BASIC,
    BETTER
}

[CreateAssetMenu(fileName = "NewMission", menuName = "TargetStrike/Mission")]
public class MissionConfig : ScriptableObject
{
    [Header("Meta")]
    public string missionName;

    [Header("City Layout")]
    public int buildingsX = 5;
    public int buildingsZ = 2;
    public float spacing = 15f;
    public Vector2 sizeXRange = new Vector2(6f, 12f);
    public Vector2 sizeZRange = new Vector2(6f, 12f);
    public Vector2 heightRange = new Vector2(15f, 40f);
    public float floorHeight = 5;
    public float floorWidth = 5;
    public int seed = 12345; // so the city is deterministic

    [Header("Target")]
    public bool targerIsCar = false;
    public TargetDescriptor target;

    [Header("World")]
    public WorldGenType worldGen = WorldGenType.BASIC;
    public bool spawnCars = false;

    [Header("Hints")]
    public List<TargetHint> hints = new();

    [Header("Preface")]
    public bool showPreface = false;
    public string prefaceTitle;
    public List<TargetHint> msgs = new();
    public Texture2D prefaceImage;
}