using UnityEngine;

[RequireComponent(typeof(Camera))]
public class Camera_Controller : MonoBehaviour
{
    [Header("Mouse Look (Pitch Only)")]
    [Tooltip("Vertical (pitch) sensitivity. Horizontal look is controlled by the FPS_Controller.")]
    public float verticalSensitivity = 300f;
    [Tooltip("Maximum up/down angle.")]
    public float verticalClampAngle = 35f;

    [Header("Camera Tilt (Z-Axis Roll)")]
    [Tooltip("Degrees to roll the camera when strafing.")]
    public float tiltAngle = 5f;
    [Tooltip("How quickly the camera tilts.")]
    public float tiltSpeed = 10f;

    [Header("Head Bob")]
    [Tooltip("Enable head bobbing.")]
    public bool enableHeadBob = true;
    [Tooltip("Speed of head bobbing.")]
    public float bobSpeed = 14f;
    [Tooltip("Amount of head bobbing.")]
    public float bobAmount = 0.05f;

    [Header("Field of View")]
    [Tooltip("Default FOV when idle.")]
    public float defaultFOV = 60f;
    [Tooltip("FOV when sprinting.")]
    public float sprintFOV = 75f;
    [Tooltip("FOV when dashing.")]
    public float dashFOV = 90f;
    [Tooltip("FOV transition speed for sprinting.")]
    public float fovTransitionSpeed = 10f;
    [Tooltip("FOV transition speed for dashing.")]
    public float dashFOVTransitionSpeed = 15f;

    [Header("Camera Shake")]
    [Tooltip("How quickly the shake intensity decays.")]
    public float shakeDecaySpeed = 5f;

    [Header("Performance Settings")]
    [Tooltip("Maximum deltaTime used for camera updates.")]
    public float maxDeltaTime = 0.033f; // ~33ms (~30 FPS)

    private Camera _cam;
    private float _xRotation = 0f; // pitch
    private float _headBobTimer = 0f;
    private Vector3 _originalLocalPos;
    private float _currentShakeIntensity = 0f;
    private Vector3 _shakeOffset = Vector3.zero;

    private EntityState _currentPlayerState = EntityState.Idle;
    private float _rollFovOffset = 0f;

    private float dt;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null)
        {
            Debug.LogError("No Camera component found on " + gameObject.name);
            enabled = false;
            return;
        }
        _cam.fieldOfView = defaultFOV;
        _originalLocalPos = transform.localPosition;
    }

    private void LateUpdate()
    {
        dt = Mathf.Min(Time.deltaTime, maxDeltaTime);
        HandleMouseLook();
        HandleCameraTilt();
        HandleHeadBob();
        HandleFOVTransition();
        HandleCameraShake();
    }

    private void HandleMouseLook()
    {
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * dt;
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -verticalClampAngle, verticalClampAngle);
    }

    private void HandleCameraTilt()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float targetZRoll = -horizontalInput * tiltAngle;
        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180f)
            currentZ -= 360f;
        float newZ = Mathf.LerpAngle(currentZ, targetZRoll, dt * tiltSpeed);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, newZ);
    }

    private void HandleHeadBob()
    {
        Vector3 targetPos = _originalLocalPos;
        if (enableHeadBob)
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool moving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
            if (moving)
            {
                _headBobTimer += dt * bobSpeed;
                targetPos.y += Mathf.Sin(_headBobTimer) * bobAmount;
            }
            else
            {
                _headBobTimer = 0f;
            }
        }
        targetPos += _shakeOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, dt * 10f);
    }

    private void HandleFOVTransition()
    {
        float targetFOV = defaultFOV;
        float transitionSpeed = fovTransitionSpeed;
        if (_currentPlayerState == EntityState.Dashing)
        {
            targetFOV = dashFOV;
            transitionSpeed = dashFOVTransitionSpeed;
        }
        else if (_currentPlayerState == EntityState.Sprinting)
        {
            targetFOV = sprintFOV;
        }
        targetFOV += _rollFovOffset;
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

    public void SetPlayerState(EntityState state)
    {
        _currentPlayerState = state;
    }

    public void SetRollFovOffset(float offset)
    {
        _rollFovOffset = offset;
    }

    public void ShakeCamera(float intensity)
    {
        if (intensity > _currentShakeIntensity)
            _currentShakeIntensity = intensity;
    }
}
