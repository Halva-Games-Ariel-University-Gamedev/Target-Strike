using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class StaticNoiseImage : MonoBehaviour
{
    [Header("Noise Texture Settings")]
    public int width = 128;
    public int height = 128;
    public float updateRate = 0.05f; // seconds between noise frames

    private RawImage _image;
    private Texture2D _texture;
    private float _timer;

    void Awake()
    {
        _image = GetComponent<RawImage>();

        _texture = new Texture2D(width, height, TextureFormat.R8, false);
        _texture.wrapMode = TextureWrapMode.Repeat;
        _texture.filterMode = FilterMode.Point;

        _image.texture = _texture;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateRate)
        {
            _timer = 0f;
            GenerateNoise();
        }
    }

    void GenerateNoise()
    {
        int pixelCount = width * height;
        Color32[] pixels = new Color32[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            byte v = (byte)Random.Range(0, 256);
            pixels[i] = new Color32(v, v, v, 255);
        }

        _texture.SetPixels32(pixels);
        _texture.Apply();
    }
}
