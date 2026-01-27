using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DroneController : MonoBehaviour
{
    [Header("Life")]
    public double battery = 100;

    [Header("Movement")]
    public float moveSpeed = 30f;
    public float boostMultiplier = 2f;
    public float flightHeight = 50f;

    [Header("Hover")]
    public float hoverAmplitude = 0.8f;   // how much it moves up/down
    public float hoverFrequency = 1.2f;   // how fast it bobs

    [Header("Inertia / Drag")]
    public float acceleration = 5f;       // how fast it reacts to input

    [Header("Camera Drag")]
    public float rotationSmoothSpeed = 10f;   // lower = more drag, higher = snappier
    private Quaternion _targetRotation;

    [Header("Sway")]
    public float maxRoll = 8f;         // left/right tilt
    public float maxPitchSway = 4f;    // forward/back tilt
    public float swayLerpSpeed = 5f;   // how fast it reacts
    private float _swayRoll;           // current roll from sway
    private float _swayPitchOffset;    // current extra pitch from sway

    [Header("Bounds (X/Z world space)")]
    public float minX = -100f;
    public float maxX = 100f;
    public float minZ = -100f;
    public float maxZ = 100f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Building Highlight")]
    public float maxSelectDistance = 1000f;
    public LayerMask buildingMask; // set to "Building" layer in Inspector

    [Header("Car Highlight")]
    public LayerMask carMask; // set to "Car" layer in Inspector
    private CarInfo _currentCarHovered;

    [Header("UI")]
    public ConfirmFireDialog confirmDialog;
    public TextMeshProUGUI dataHits;



    private bool _inputEnabled = true;

    private float _yaw;
    private float _pitch;

    private Vector3 _velocity;  // current horizontal velocity

    private BuildingInfo _currentHovered;

    void Start()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x;

        Vector3 pos = transform.position;
        pos.y = flightHeight;
        transform.position = pos;

        _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.rotation = _targetRotation;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!_inputEnabled)
            return;  // don't move / look around

        HandleMouseLook();
        HandleMovement();
        HandleHighlight();
        HandleFireClick();
        UpdateVisualData();
        CheckTimer();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void ConfirmFire(BuildingInfo target)
    {
        if (target == null) return;

        Debug.Log("FIRING on building id: " + target.id);

        var mission = MissionsMenu.Instance.CurrentMission;
        Debug.Log("About to play explosion");
        AudioManager.Instance.Play("explosion");
        Debug.Log("After play call");

        if (target.gridX == mission.target.x && target.gridZ == mission.target.z)
        {
            IEnumerator LoadNextAfterRealtime(float delay)
            {
                yield return new WaitForSecondsRealtime(delay);
                MissionsMenu.Instance.LoadNextMissionOrBackToMenu();
            }

            StartCoroutine(LoadNextAfterRealtime(1));
        }
        else
        {
            IEnumerator LoadNextAfterRealtime(float delay)
            {
                yield return new WaitForSecondsRealtime(delay);
                MissionsMenu.Instance.ToLoseScreen();
            }

            StartCoroutine(LoadNextAfterRealtime(1));
        }

        // TODO:
        // - play explosion
        // - check if target.isTarget
        // - end mission / show success/fail UI, etc.
    }

    public void ConfirmFire(CarInfo target)
    {
        if (target == null) return;

        Debug.Log("FIRING on building id: " + target.id);

        var mission = MissionsMenu.Instance.CurrentMission;
        Debug.Log("About to play explosion");
        AudioManager.Instance.Play("explosion");
        Debug.Log("After play call");

        if (target.id == mission.target.id)
        {
            IEnumerator LoadNextAfterRealtime(float delay)
            {
                yield return new WaitForSecondsRealtime(delay);
                MissionsMenu.Instance.LoadNextMissionOrBackToMenu();
            }

            StartCoroutine(LoadNextAfterRealtime(1));
        }
        else
        {
            IEnumerator LoadNextAfterRealtime(float delay)
            {
                yield return new WaitForSecondsRealtime(delay);
                MissionsMenu.Instance.ToLoseScreen();
            }

            StartCoroutine(LoadNextAfterRealtime(1));
        }

        // TODO:
        // - play explosion
        // - check if target.isTarget
        // - end mission / show success/fail UI, etc.
    }

    public void UpdateVisualData()
    {
        var bc = _currentHovered?.GetComponent<BoxCollider>() ?? null;

        string txt = "";

        if (bc != null)
        {
            txt = @$"BUILDING ({_currentHovered?.id}) DATA -
                HEIGHT - {bc?.size.y}M
                DIM - {bc?.size.x}M x {bc?.size.z}M";
        }
        else
        {
            if (_currentCarHovered != null)
            {
                txt = @$"CAR ({_currentCarHovered?.id}) DATA -
                    LINCESE PLATE - {_currentCarHovered.lincensePlate}
                    COLOR - {_currentCarHovered.carColor}";
            }
            else txt = "NULL - NO DATA";
        }

        dataHits.SetText($@"
            DATA
            DRONE BATTERY - {Math.Round(battery)} %
            
            {txt}
        ".Replace("    ", ""));
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        // target rotation from yaw/pitch
        _targetRotation = Quaternion.Euler(
            _pitch + _swayPitchOffset,   // base pitch + sway
            _yaw,                        // yaw as usual
            _swayRoll                    // roll from sway
        );

        // smooth towards target (this is the "drag")
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRotation,
            rotationSmoothSpeed * Time.deltaTime
        );
    }

    private void HandleFireClick()
    {
        // no input when disabled (dialog open, etc.)
        if (!_inputEnabled)
            return;

        // left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            if (_currentCarHovered != null && confirmDialog != null)
            {
                confirmDialog.Show(_currentCarHovered);
            }

            // only if we're actually looking at a building
            if (_currentHovered != null && confirmDialog != null)
            {
                confirmDialog.Show(_currentHovered);
            }
        }
    }

    void HandleMovement()
    {
        // WASD input
        float h = Input.GetAxisRaw("Horizontal"); // A,D
        float v = Input.GetAxisRaw("Vertical");   // W,S

        // --- NEW: sway based on input ---
        float targetRoll = -h * maxRoll;          // A = roll right, D = roll left (flip if you want)
        float targetPitchSway = v * maxPitchSway;     // W = nose down a bit, S = nose up a bit

        _swayRoll = Mathf.Lerp(_swayRoll, targetRoll, swayLerpSpeed * Time.deltaTime);
        _swayPitchOffset = Mathf.Lerp(_swayPitchOffset, targetPitchSway, swayLerpSpeed * Time.deltaTime);
        // --- END NEW ---

        // Horizontal forward/right (ignore vertical)
        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0f;
        right.Normalize();

        Vector3 desiredDir = (forward * v + right * h).normalized;

        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= boostMultiplier;

        Vector3 targetVelocity = desiredDir * currentSpeed;

        _velocity = Vector3.Lerp(_velocity, targetVelocity, acceleration * Time.deltaTime);

        Vector3 pos = transform.position + _velocity * Time.deltaTime;

        // hover
        float hoverOffset = Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        pos.y = flightHeight + hoverOffset;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);

        transform.position = pos;
    }

    void HandleHighlight()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        // Try hit car first (or building first, your choice)
        if (Physics.Raycast(ray, out RaycastHit hitCar, maxSelectDistance, carMask))
        {
            CarInfo car = hitCar.collider.GetComponentInParent<CarInfo>();

            if (car != _currentCarHovered)
            {
                SetHoveredCar(car);
            }

            // If we are on a car, clear building hover
            SetHovered(null);
            return;
        }

        // Then buildings
        if (Physics.Raycast(ray, out RaycastHit hitB, maxSelectDistance, buildingMask))
        {
            BuildingInfo info = hitB.collider.GetComponentInParent<BuildingInfo>();
            if (info != _currentHovered)
                SetHovered(info);

            // Clear car hover
            SetHoveredCar(null);
            return;
        }

        // Nothing hit
        SetHovered(null);
        SetHoveredCar(null);
    }

    void SetHoveredCar(CarInfo newHovered)
    {
        if (_currentCarHovered == newHovered) return;

        if (_currentCarHovered != null)
            _currentCarHovered.SetHighlighted(false);

        _currentCarHovered = newHovered;

        if (_currentCarHovered != null)
            _currentCarHovered.SetHighlighted(true);
    }


    void CheckTimer()
    {
        battery -= 1 / 1.2 * Time.deltaTime;

        if (battery <= 0)
        {
            MissionsMenu.Instance.ToLoseScreen();
        }
    }

    void SetHovered(BuildingInfo newHovered)
    {
        if (_currentHovered == newHovered) return;

        if (_currentHovered != null)
            _currentHovered.SetHighlighted(false);

        _currentHovered = newHovered;

        if (_currentHovered != null)
            _currentHovered.SetHighlighted(true);
    }
}
