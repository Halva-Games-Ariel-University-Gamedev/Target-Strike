using UnityEngine;
using UnityEngine.UI;

public class StaticNoise : MonoBehaviour
{
    public float jitterSpeed = 20f;   // how fast the static changes
    public float tiling = 2f;         // how much it tiles

    private RawImage _image;
    private Rect _baseRect;

    void Awake()
    {
        _image = GetComponent<RawImage>();
        _baseRect = _image.uvRect;
    }

    void Update()
    {
        // small random offset each frame for TV-like static
        float offsetX = Random.value * jitterSpeed;
        float offsetY = Random.value * jitterSpeed;

        _image.uvRect = new Rect(
            offsetX,
            offsetY,
            tiling,
            tiling
        );
    }
}