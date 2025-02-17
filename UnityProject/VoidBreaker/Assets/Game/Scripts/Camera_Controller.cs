using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Camera_Controller : MonoBehaviour
{
    [Header("Mouse Look")]
    [Tooltip("Overall mouse look sensitivity.")]
    public float verticalSensitivity = 300f;
    public float horizontalSensitivity = 600f;
    [Tooltip("Maximum up/down angle.")]
    public float verticalClampAngle = 35f;

    [Header("Camera Tilt (Z-Axis Roll)")]
    [Tooltip("How many degrees to roll the camera when strafing.")]
    public float tiltAngle = 5f;
    [Tooltip("How quickly the camera rolls.")]
    public float tiltSpeed = 10f;

    [Header("Head Bob")]
    public bool enableHeadBob = true;
    public float bobSpeed = 14f;
    public float bobAmount = 0.05f;

    [Header("Field of View")]
    [Tooltip("Default FOV when not sprinting or dashing.")]
    public float defaultFOV = 60f;
    [Tooltip("FOV when sprinting.")]
    public float sprintFOV = 75f;
    [Tooltip("FOV when dashing.")]
    public float dashFOV = 90f;
    [Tooltip("How quickly FOV transitions when sprinting.")]
    public float fovTransitionSpeed = 10f;
    [Tooltip("How quickly FOV transitions when dashing.")]
    public float dashFOVTransitionSpeed = 15f;

    [Header("Camera Shake")]
    [Tooltip("How quickly shake intensity decays.")]
    public float shakeDecaySpeed = 5f;

    [Header("Performance Settings")]
    [Tooltip("Maximum deltaTime used for camera updates to avoid large jumps due to lag spikes.")]
    public float maxDeltaTime = 0.033f; // ~33ms (approx 30 FPS)

    [Header("References")]
    [Tooltip("Typically the parent object for horizontal rotation. " +
             "This script handles vertical rotation on the Camera itself.")]
    public Transform playerOrientation;

    // Internal variables
    private Camera _cam;
    private float _xRotation = 0f;
    private float _yRotation = 0f;
    private float _headBobTimer;
    private Vector3 _originalLocalPos;
    private float _currentShakeIntensity;
    private Vector3 _shakeOffset;

    // Reference to the FPS_Controller to detect state (e.g., dashing).
    private FPS_Controller fpsController;

    // Cached deltaTime for this frame, clamped to maxDeltaTime.
    private float dt;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam)
        {
            Debug.LogError("No Camera component found on " + gameObject.name);
            enabled = false;
            return;
        }
        _originalLocalPos = transform.localPosition;
        _cam.fieldOfView = defaultFOV;

        if (playerOrientation != null)
        {
            _yRotation = playerOrientation.eulerAngles.y;
        }

        // Attempt to retrieve the FPS_Controller from the parent.
        fpsController = GetComponentInParent<FPS_Controller>();
    }

    private void Update()
    {
        // Clamp Time.deltaTime to prevent large jumps during lag spikes.
        dt = Mathf.Min(Time.deltaTime, maxDeltaTime);

        HandleMouseLook();
        HandleCameraTilt();
        HandleHeadBob();
        HandleFOVTransition();
        HandleCameraShake();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * horizontalSensitivity * dt;
        float mouseY = Input.GetAxisRaw("Mouse Y") * verticalSensitivity * dt;

        _yRotation += mouseX;
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -verticalClampAngle, verticalClampAngle);

        if (playerOrientation != null)
        {
            // Optionally, apply horizontal rotation to the player orientation.
            // Example: playerOrientation.rotation = Quaternion.Euler(0f, _yRotation, 0f);
        }
    }

    private void HandleCameraTilt()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float targetZRoll = -horizontalInput * tiltAngle;

        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        float newZ = Mathf.LerpAngle(currentZ, targetZRoll, dt * tiltSpeed);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, newZ);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isMoving = (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f);

        if (isMoving)
        {
            _headBobTimer += dt * bobSpeed;
            float bobOffset = Mathf.Sin(_headBobTimer) * bobAmount;
            transform.localPosition = _originalLocalPos + _shakeOffset + new Vector3(0f, bobOffset, 0f);
        }
        else
        {
            _headBobTimer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                _originalLocalPos + _shakeOffset,
                dt * bobSpeed
            );
        }
    }

    private void HandleFOVTransition()
    {
        // Determine sprinting state (using Left Shift as an example)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        // Check if the player is dashing via the player's state.
        bool isDashing = fpsController != null && fpsController.CurrentState == PlayerState.Dashing;

        float targetFOV = defaultFOV;
        float transitionSpeed = fovTransitionSpeed;

        if (isDashing)
        {
            targetFOV = dashFOV;
            transitionSpeed = dashFOVTransitionSpeed;
        }
        else if (isSprinting)
        {
            targetFOV = sprintFOV;
        }

        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFOV, dt * transitionSpeed);
    }

    private void HandleCameraShake()
    {
        if (_currentShakeIntensity > 0)
        {
            _shakeOffset = Random.insideUnitSphere * _currentShakeIntensity;
            _currentShakeIntensity -= shakeDecaySpeed * dt;
            if (_currentShakeIntensity < 0)
            {
                _currentShakeIntensity = 0;
                _shakeOffset = Vector3.zero;
            }
        }
    }

    public void ShakeCamera(float intensity)
    {
        if (intensity > _currentShakeIntensity)
        {
            _currentShakeIntensity = intensity;
        }
    }
}
