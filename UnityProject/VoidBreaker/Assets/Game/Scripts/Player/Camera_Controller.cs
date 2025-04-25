using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

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


    [Header("Post-Processing Effects")]
    [Tooltip("Enable post-processing effects.")]
    public bool enablePostProcessing = true;
    public GameObject postProcessingVolume;
    private float volumeIntensity = 0f;

    [Header("Speed Lines")]
    [Tooltip("Enable speed lines.")]
    public bool enableSpeedLines = true;

    public Material speedLinesMaterial;

    // --- Added for Blade Mode Integration ---
    [HideInInspector]
    public bool overrideFOV = false;
    [HideInInspector]
    public float targetOverrideFOV;

    private Camera _cam;
    private float _xRotation = 0f; // pitch
    private float _yRotation = 0f; // yaw
    private float _headBobTimer = 0f;
    private Vector3 _originalLocalPos;
    private float _currentShakeIntensity = 0f;
    private Vector3 _shakeOffset = Vector3.zero;

    private EntityState _currentPlayerState = EntityState.Idle;
    private float _rollFovOffset = 0f;

    private float dt;

    private float timeMultiplier = 1.0f;

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
        SetPostProcessing(enablePostProcessing);
    }

    private void LateUpdate()
    {
        if (Game_Manager.IsCursorLocked() == false)
        {
            // If the cursor is not locked, we dont want to do anything else.
            return;
        }
        dt = Mathf.Min(Time.deltaTime * timeMultiplier, maxDeltaTime * timeMultiplier);
        HandleCameraTilt();
        HandleHeadBob();
        HandleFOVTransition();
        HandleCameraShake();
        HandlePostProcess();
    }



    // Usage example
    public void setSpeedlineOpacity(float opacity)
    {
        if (speedLinesMaterial != null)
        {
            float mappedOpacity = Mathf.Lerp(0f, 0.4f, Mathf.Clamp01(opacity));
            speedLinesMaterial.SetFloat("_Line_Density", mappedOpacity);
        }
    }

    private void HandleCameraTilt()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float targetZRoll = -horizontalInput * tiltAngle;
        float currentZ = transform.localEulerAngles.z;
        if (currentZ > 180f)
            currentZ -= 360f;
        float newZ = Mathf.LerpAngle(currentZ, targetZRoll, dt * tiltSpeed);
        transform.localRotation = Quaternion.Euler(_xRotation, _yRotation, newZ);
    }
    private void HandlePostProcess()
    {
        if (enablePostProcessing)
        {
            if (postProcessingVolume != null)
            {
                postProcessingVolume.SetActive(true);
                Volume volume = postProcessingVolume.GetComponent<Volume>();
                volume.weight = Mathf.Clamp(Mathf.Lerp(volume.weight, volumeIntensity, dt * 10f), 0f, 1f);
            }
        }
        else
        {
            if (postProcessingVolume != null)
            {
                postProcessingVolume.SetActive(false);
            }
        }
    }

    public void SetVolumeIntensity(float intensity)
    {
        volumeIntensity = intensity;
    }

    public void SetPostProcessing(bool enabled)
    {
        enablePostProcessing = enabled;
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
        float targetFOV;
        float transitionSpeed = fovTransitionSpeed;

        if (overrideFOV)
        {
            targetFOV = targetOverrideFOV;
        }
        else
        {
            targetFOV = defaultFOV;
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

    public void SetPlayerState(EntityState state)
    {
        _currentPlayerState = state;
    }

    public void SetRollFovOffset(float offset)
    {
        _rollFovOffset = offset;
    }

    /// <summary>
    /// Call this to trigger a camera shake with the given intensity.
    /// </summary>
    public void ShakeCamera(float intensity)
    {
        if (intensity > _currentShakeIntensity)
            _currentShakeIntensity = intensity;
    }

    /// <summary>
    /// Enables FOV override with the given value.
    /// </summary>
    public void SetOverrideFOV(float newFOV)
    {
        overrideFOV = true;
        targetOverrideFOV = newFOV;
    }

    /// <summary>
    /// Clears any FOV override so that normal FOV transitions resume.
    /// </summary>
    public void ClearOverrideFOV()
    {
        overrideFOV = false;
    }


    public void setXrot(float x)
    {
        _yRotation = x;
    }

    public float xRot
    {
        get { return _yRotation; }
        set { _yRotation = value; }
    }

    public float yRot
    {
        get { return _xRotation; }
        set
        {   
            _xRotation = value;
            _xRotation = Mathf.Clamp(_xRotation, -verticalClampAngle, verticalClampAngle);
        }
    }

    public float timeFlow
    {
        get { return timeMultiplier; }
        set { timeMultiplier = value; }
    }

}
