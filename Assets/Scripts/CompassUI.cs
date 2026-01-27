using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class CompassTape : MonoBehaviour
{
    [Header("Target (camera/drone)")]
    public Transform target;               // object whose Y-rotation is the heading

    [Header("Tick Setup")]
    public RectTransform ticksParent;      // usually the CompassBar rect
    public RectTransform tickPrefab;       // DISABLED TickPrefab (has TMP child)

    [Tooltip("Degrees between ticks. Use a value that divides 360.")]
    public float degreesPerTick = 15f;     // 15° per tick

    [Tooltip("Horizontal spacing (in pixels) between neighboring ticks.")]
    public float pixelsBetweenTicks = 120f; // distance between ticks in UI

    [Header("Tick Style")]
    public float minorHeight = 10f;
    public float majorHeight = 22f;        // used for 45° multiples
    public float heightOffset = 10;

    // ────────────────────────────────────────────────

    private class Tick
    {
        public RectTransform rect;
        public TMP_Text label;
        public float angle;                // 0..360 world angle this tick represents
    }

    private readonly List<Tick> _ticks = new();
    private RectTransform _rect;
    private float _pixelsPerDegree;
    private float _visibleAngleRange;      // in degrees, auto-calculated

    void Awake()
    {
        _rect = (RectTransform)transform;

        if (!ticksParent)
            ticksParent = _rect;

        if (!tickPrefab)
        {
            Debug.LogError("CompassTape: Tick prefab not assigned.");
            enabled = false;
            return;
        }

        tickPrefab.gameObject.SetActive(false);

        // create ticks for the full 360°
        if (degreesPerTick <= 0f) degreesPerTick = 15f;
        int totalTicks = Mathf.RoundToInt(360f / degreesPerTick);

        for (int i = 0; i < totalTicks; i++)
        {
            RectTransform t = Instantiate(tickPrefab, ticksParent);
            t.gameObject.SetActive(true);

            var tick = new Tick
            {
                rect = t,
                label = t.GetComponentInChildren<TMP_Text>(true),
                angle = Mathf.Repeat(i * degreesPerTick, 360f)
            };

            _ticks.Add(tick);
        }

        _ticks.Reverse();

        RecalculateSpacing();
    }

    void OnRectTransformDimensionsChange()
    {
        RecalculateSpacing();
    }

    void RecalculateSpacing()
    {
        if (!ticksParent || degreesPerTick <= 0f || pixelsBetweenTicks <= 0f)
            return;

        float width = ticksParent.rect.width;
        if (width <= 0f) width = 1f;

        // pixels per one degree, based on desired spacing between ticks
        _pixelsPerDegree = pixelsBetweenTicks / degreesPerTick;

        // how many degrees fit from center to one side
        float halfWidth = width * 0.5f;
        _visibleAngleRange = halfWidth / _pixelsPerDegree;
    }

    void Update()
    {
        if (!target) return;

        // current heading 0..360
        float heading = Mathf.Repeat(target.eulerAngles.y, 360f);

        // how many ticks per 45° (for N/E/S/W and 45° labels)
        int stepsPer10 = Mathf.Max(1, Mathf.RoundToInt(10f / degreesPerTick));

        foreach (var tick in _ticks)
        {
            // signed angle difference between forward and tick direction (-180..180)
            float diff = Mathf.DeltaAngle(heading, tick.angle);

            // hide if too far from center
            if (Mathf.Abs(diff) > _visibleAngleRange + degreesPerTick)
            {
                if (tick.rect.gameObject.activeSelf)
                    tick.rect.gameObject.SetActive(false);
                continue;
            }

            if (!tick.rect.gameObject.activeSelf)
                tick.rect.gameObject.SetActive(true);

            // position in UI
            float x = diff * _pixelsPerDegree;
            tick.rect.anchoredPosition = new Vector2(x, heightOffset);

            // major tick every 45°
            int tickIndex = Mathf.RoundToInt(tick.angle / degreesPerTick);
            bool isMajor = (tickIndex % stepsPer10 == 0);

            // height
            float h = isMajor ? majorHeight : minorHeight;
            tick.rect.sizeDelta = new Vector2(tick.rect.sizeDelta.x, h);

            // label (TMP) for 45° multiples only
            if (tick.label != null)
            {
                if (isMajor)
                {
                    int angleInt = Mathf.RoundToInt(tick.angle) % 360;
                    if (angleInt < 0) angleInt += 360;

                    switch (angleInt)
                    {
                        case 0: tick.label.text = "N"; break;
                        case 90: tick.label.text = "E"; break;
                        case 180: tick.label.text = "S"; break;
                        case 270: tick.label.text = "W"; break;
                        default:
                            tick.label.text = angleInt.ToString();
                            break;
                    }

                    tick.label.enabled = true;
                }
                else
                {
                    tick.label.enabled = false;
                }
            }
        }
    }
}
