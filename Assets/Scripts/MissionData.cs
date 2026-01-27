using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class TargetHint
{
    [TextArea]
    public string text;
}

[Serializable]
public class MissionMessages
{
    [TextArea]
    public string text;
}


[Serializable]
public class TargetDescriptor
{
    [Header("Grid Position (index in the city grid)")]
    [Tooltip("0 .. buildingsX-1")]
    public int x;
    [Tooltip("0 .. buildingsZ-1")]
    public int z;

    [Tooltip("ONLY FOR CARS")]
    public int id = -1;

    [Header("Size Override")]
    [Tooltip("If false, target uses the normal random building size.")]
    public bool overrideSize = false;

    [Tooltip("Width along X (only used if overrideSize = true).")]
    public float widthX = 1;

    [Tooltip("Depth along Z (only used if overrideSize = true).")]
    public float depthZ = 1;

    [Tooltip("Height (only used if overrideSize = true).")]
    public float height = 1;
}