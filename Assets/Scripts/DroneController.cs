using UnityEngine;
using UnityEngine.SceneManagement;

public class DroneController : MonoBehaviour
{
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

    [Header("UI")]
    public ConfirmFireDialog confirmDialog;

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

        if (target.gridX == mission.target.x && target.gridZ == mission.target.z)
        {
            MissionsMenu.Instance.LoadNextMissionOrBackToMenu();
        }
        else
        {
            MissionsMenu.Instance.Reset();
        }

        // TODO:
        // - play explosion
        // - check if target.isTarget
        // - end mission / show success/fail UI, etc.
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
        if (Physics.Raycast(ray, out RaycastHit hit, maxSelectDistance, buildingMask))
        {
            BuildingInfo info = hit.collider.GetComponentInParent<BuildingInfo>();

            if (info != _currentHovered)
            {
                SetHovered(info);
            }
        }
        else
        {
            SetHovered(null);
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
