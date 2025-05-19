using System;
using UnityEngine;

[Serializable]
public class PID
{
    private float _p, _i, _d;
    private float _kp, _ki, _kd;
    private float _prevError;

    /// <summary>
    /// Proportional gain
    /// </summary>
    public float Kp { get { return _kp; } set { _kp = value; } }
    /// <summary>
    /// Integral gain
    /// </summary>
    public float Ki { get { return _ki; } set { _ki = value; } }
    /// <summary>
    /// Derivative gain
    /// </summary>
    public float Kd { get { return _kd; } set { _kd = value; } }

    public PID(float p, float i, float d)
    {
        _kp = p;
        _ki = i;
        _kd = d;
    }

    /// <summary>
    /// Compute control output given current error and elapsed time.
    /// </summary>
    public float GetOutput(float currentError, float deltaTime)
    {
        _p = currentError;
        _i += _p * deltaTime;
        _d = deltaTime > 0f ? (_p - _prevError) / deltaTime : 0f;
        _prevError = _p;
        return _p * Kp + _i * Ki + _d * Kd;
    }

    /// <summary>
    /// Reset integral and derivative history.
    /// </summary>
    public void Reset()
    {
        _i = 0f;
        _prevError = 0f;
    }
}

public class AI_Movement_Controller : Entity
{
    [Header("Steering PID")]
    [Tooltip("Proportional gain for yaw control")]
    [SerializeField] private float yawKp = 5f;
    [Tooltip("Integral gain for yaw control")]
    [SerializeField] private float yawKi = 0f;
    [Tooltip("Derivative gain for yaw control")]
    [SerializeField] private float yawKd = 0.5f;

    private PID _yawController;
    private float aiLookYaw;

    protected override void Awake()
    {
        base.Awake();  // Initialize Rigidbody, CapsuleCollider, etc.
        _yawController = new PID(yawKp, yawKi, yawKd);
    }

    protected override void ProcessInput()
    {
        // Time step (clamped by Entity.maxDeltaTime)
        float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);

        // Compute shortest angular error between current and desired yaw
        float currentYaw = transform.eulerAngles.y;
        float error = Mathf.DeltaAngle(currentYaw, aiLookYaw);

        // PID output is a desired yaw rate (degrees/sec)
        float controlSignal = _yawController.GetOutput(error, dt);

        // Apply rotation: rotate by (rate * dt) around Y axis
        transform.Rotate(0f, controlSignal * dt, 0f, Space.World);

    }

    /// <summary>
    /// Called each frame by GOAPAgent to feed movement inputs.
    /// </summary>
    public void SetAIInput(float horizontal, float vertical,
                           bool sprint, bool crouch, bool jump, bool dash,
                           float lookYaw)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
        wantSprint = sprint;
        wantCrouch = crouch;
        wantJump = jump;
        wantDash = dash;

        // Whenever the desired yaw changes significantly, reset PID history
        if (Mathf.Abs(Mathf.DeltaAngle(aiLookYaw, lookYaw)) > 10f)
            _yawController.Reset();

        aiLookYaw = lookYaw;
    }

    protected override void Die()
    {
        NotifyDeath();
        Destroy(gameObject);
    }
}
