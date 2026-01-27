using System;
using System.Text;
using UnityEngine;

public class CarInfo : MonoBehaviour
{
    public int id;
    public string carColor;

    public string lincensePlate;

    [Header("Highlight")]
    public float highlightBoost = 1f;

    [SerializeField] Renderer[] renderers;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    Color _baseColor = Color.white;
    MaterialPropertyBlock _mpb;

    private GameObject outlineMesh;

    public void Init(int newId, string colorName, Color baseColor, Renderer[] rs = null)
    {
        id = newId;
        carColor = colorName;
        _baseColor = baseColor;

        if (rs != null && rs.Length > 0) renderers = rs;
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        lincensePlate = LicensePlateGenerator.Generate(id);

        ApplyColor(_baseColor);

        outlineMesh = outlineMesh = transform.Find("outline")?.gameObject;

    }

    public void SetHighlighted(bool on)
    {
        if (outlineMesh != null)
            outlineMesh.SetActive(on);
    }

    void ApplyColor(Color c)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r) continue;

            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(_mpb);
        }
    }
}

public static class LicensePlateGenerator
{
    // Avoid easy-problem letters in plates (optional)
    private const string Letters = "ABCDEFGHJKLMNPRSTUVWXYZ"; // removed I,O,Q (look like 1/0)
    private const string Digits = "0123456789";

    // Example formats (add/remove as you like):
    // "LLL-DDD"  => ABC-123
    // "LL-DDDD"  => AB-4821
    // "DDD-LLL"  => 572-ZKP
    // "LLL-DDDD" => QRT-9021
    private static readonly string[] Formats =
    {
        "LLL-DDD",
        "LL-DDDD",
        "DDD-LLL",
        "LLL-DDDD",
        "DD-LLL-DD"
    };

    /// <summary>
    /// Generate a random license plate.
    /// If you pass a seed, output is deterministic for that seed.
    /// </summary>
    public static string Generate(int? seed = null)
    {
        System.Random rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        string format = Formats[rng.Next(Formats.Length)];
        var sb = new StringBuilder(format.Length);

        foreach (char ch in format)
        {
            switch (ch)
            {
                case 'L':
                    sb.Append(Letters[rng.Next(Letters.Length)]);
                    break;
                case 'D':
                    sb.Append(Digits[rng.Next(Digits.Length)]);
                    break;
                default:
                    // Keep separators like '-' or ' '
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generate multiple unique plates.
    /// </summary>
    public static string[] GenerateMany(int count, int? seed = null)
    {
        var rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        var set = new System.Collections.Generic.HashSet<string>();
        var list = new System.Collections.Generic.List<string>(count);

        int safety = Math.Max(200, count * 20);
        while (list.Count < count && safety-- > 0)
        {
            // Vary seed per attempt so format choice isn't stuck
            string plate = Generate(rng.Next());
            if (set.Add(plate))
                list.Add(plate);
        }

        return list.ToArray();
    }
}
