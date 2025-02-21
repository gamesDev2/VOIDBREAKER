using UnityEngine;

public enum PlayerState
{
    Idle,
    Walking,
    Sprinting,
    Crouching,
    Dashing,
    Jumping,
    WallRunning,
    Sliding,
    RailRiding,
    Rolling
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FPS_Controller : MonoBehaviour
{
    #region Public Fields

    [Header("Hierarchy References")]
    [Tooltip("Drag the 'Head' transform here (child of the Player).")]
    public Transform head;

    [Header("Camera Roll and FOV Settings")]
    [Tooltip("Reference to the player's Camera.")]
    public Camera playerCamera;
    [Tooltip("Base FOV of the camera.")]
    public float baseFov = 60f;
    [Tooltip("Maximum additional FOV for extreme falls.")]
    public float maxFovIncrease = 20f;
    [Tooltip("Multiplier to calculate FOV increase based on fall duration.")]
    public float fovMultiplier = 10f;
    [Tooltip("Maximum camera roll angle (in degrees).")]
    public float maxCameraRollAngle = 20f;
    [Tooltip("Multiplier to calculate camera roll based on fall duration.")]
    public float rollMultiplier = 10f;

    [Header("Mouse Settings")]
    [Tooltip("Only used for horizontal rotation (yaw).")]
    public float mouseSensitivity = 100f;

    [Header("Movement Settings")]
    [Tooltip("Base walking speed.")]
    public float moveSpeed = 5f;
    public float groundDrag = 6f;
    [Tooltip("Force applied when jumping.")]
    public float jumpForce = 5f;
    [Tooltip("Seconds before you can jump again.")]
    public float jumpCooldown = 0.25f;
    [Tooltip("Multiplier for horizontal movement while in the air.")]
    public float airMultiplier = 0.5f;

    [Header("Double Jump Settings")]
    [Tooltip("Maximum number of jumps (including the initial jump).")]
    public int maxJumpCount = 2;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode dashKey = KeyCode.E;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Sprinting Settings")]
    [Tooltip("Speed when sprinting.")]
    public float sprintSpeed = 8f;

    [Header("Ground Check")]
    [Tooltip("Used for the raycast length (e.g., the player’s capsule height).")]
    public float playerHeight = 2f;
    [Tooltip("Which layers count as ground?")]
    public LayerMask whatIsGround;

    [Header("Crouch Settings")]
    [Tooltip("Collider height when crouching.")]
    public float crouchHeight = 1f;
    [Tooltip("Movement speed when crouching.")]
    public float crouchSpeed = 3f;
    [Tooltip("How quickly to transition between crouch and stand.")]
    public float crouchTransitionSpeed = 8f;
    [Tooltip("How far (in local Y) to lower the camera when crouched.")]
    public float crouchCameraHeight = 0.5f;

    [Header("Speed Settings")]
    [Tooltip("Maximum speed achievable during a dash.")]
    public float maxSpeed = 10f;

    [Header("Dash Settings")]
    [Tooltip("Force applied when dashing.")]
    public float dashForce = 15f;
    [Tooltip("Duration of the dash effect.")]
    public float dashDuration = 0.2f;
    [Tooltip("Cooldown before you can dash again.")]
    public float dashCooldown = 2f;

    [Header("Wall Run Settings")]
    [Tooltip("Launch speed along the wall (orthogonal to the surface normal).")]
    public float wallRunLaunchSpeed = 10f;
    [Tooltip("Acceleration applied along the wall while wall running.")]
    public float wallRunAcceleration = 5f;
    [Tooltip("Maximum horizontal speed achievable while wall running.")]
    public float maxWallRunSpeed = 20f;
    [Tooltip("Reduced gravity multiplier while wall running (0 = no gravity, 1 = normal gravity).")]
    public float wallRunGravity = 0.3f;
    [Tooltip("Maximum duration for a wall run.")]
    public float maxWallRunTime = 2f;
    [Tooltip("Distance for the raycast to detect walls.")]
    public float wallRunRayDistance = 1.0f;

    [Header("Slide Settings")]
    [Tooltip("Slide is essentially a dash that forces crouch height.")]
    public float slideSpeed = 12f;

    [Header("Rail Riding Settings")]
    [Tooltip("Offset to position the player on top of the rail.")]
    public float railVerticalOffset = 1f;

    [Header("High Fall Roll Settings")]
    [Tooltip("Enable or disable the high fall roll feature.")]
    public bool enableHighFallRoll = true;
    [Tooltip("Minimum fall distance (in meters) needed to trigger a roll.")]
    public float highFallRollThreshold = 5f;
    [Tooltip("Duration of the roll in seconds.")]
    public float rollDuration = 0.5f;
    [Tooltip("Forward impulse applied when rolling.")]
    public float rollForce = 5f;

    [Header("Non-Wall Runnable Push Settings")]
    [Tooltip("Force to push the player downward if colliding with a non-wall runnable object while in air and pressing movement.")]
    public float pushForce = 10f;
    [Tooltip("Multiplier to expand the sphere radius for collision detection.")]
    public float sphereRadiusMultiplier = 1.2f;
    [Tooltip("Height threshold (player's Y position) above which the push is applied.")]
    public float pushHeightThreshold = 2f;
    [Tooltip("Minimum time (in seconds) between push events.")]
    public float pushCooldown = 1f;

    [Header("Ground Pound Settings")]
    [Tooltip("Downward force applied when ground pounding.")]
    public float groundPoundForce = 20f;

    [Header("Performance Settings")]
    [Tooltip("Maximum deltaTime to use for Update calculations (to handle FPS spikes).")]
    public float maxDeltaTime = 0.016f; // ~60 fps

    #endregion

    #region Private Fields

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    private float horizontalInput;
    private float verticalInput;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private float originalMoveSpeed;
    private Vector3 standHeadLocalPos;
    private Vector3 crouchHeadLocalPos;

    private bool readyToJump = true;
    private int jumpCount = 0;
    private bool isCrouching = false;
    private bool grounded = false;

    private bool canDash = true;
    private bool isDashing = false;

    private float wallRunTimer = 0f;
    private bool isWallRunning = false;
    private Vector3 currentWallNormal;

    private bool isSliding = false;
    private bool isOnRail = false;
    private Rail currentRail = null;

    private float fallStartHeight;
    private float fallStartTime;
    private bool fallStarted = false;

    private float rollEffectStartTime;
    private float targetCameraFovIncrease;
    private float targetCameraRollAngle;
    private bool isRolling = false;

    private float lastPushTime = 0f;
    private bool isGroundPounding = false;

    private PlayerState currentState = PlayerState.Idle;
    public PlayerState CurrentState { get { return currentState; } }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
            Debug.LogError("No CapsuleCollider found on the player!");

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
        originalMoveSpeed = moveSpeed;
        standHeadLocalPos = head.localPosition;
        crouchHeadLocalPos = new Vector3(standHeadLocalPos.x, standHeadLocalPos.y - crouchCameraHeight, standHeadLocalPos.z);
        currentState = PlayerState.Idle;

        if (playerCamera == null)
            playerCamera = head.GetComponent<Camera>();
        if (playerCamera != null)
            playerCamera.fieldOfView = baseFov;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        readyToJump = true;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);
        ProcessMouseLook(dt);
        ProcessGroundCheck();
        ProcessFallRoll(dt);
        ProcessMovementInput();
        ProcessJumpInput();
        ProcessCrouchAndGroundPoundInput();
        ProcessDashInput();
        ProcessWallRunCheck(dt);
        SmoothHeadAndCollider(dt);
        UpdateCurrentWalkSpeed();
        UpdatePlayerState();
        UpdateCameraController();
    }

    private void FixedUpdate()
    {
        if (isRolling)
        {
            SpeedControl();
            return;
        }

        if (isOnRail && currentRail != null)
        {
            // Find the closest point on the rail and force its vertical position.
            Vector3 railPoint = currentRail.GetClosestPointOnRail(rb.position);
            railPoint.y += railVerticalOffset;

            // Smoothly interpolate horizontal position toward the rail while snapping Y.
            Vector3 newPos = rb.position;
            newPos.x = Mathf.Lerp(rb.position.x, railPoint.x, 0.2f);
            newPos.z = Mathf.Lerp(rb.position.z, railPoint.z, 0.2f);
            newPos.y = railPoint.y;
            rb.MovePosition(newPos);

            // Get the rail's horizontal direction.
            Vector3 railDir = currentRail.GetRailDirection();
            railDir = Vector3.ProjectOnPlane(railDir, Vector3.up).normalized;

            // Reverse direction if the player is facing opposite.
            if (Vector3.Dot(transform.forward, railDir) < 0f)
            {
                railDir = -railDir;
            }

            // Set horizontal velocity only.
            rb.velocity = railDir * currentRail.railSpeed;
            // Force vertical velocity to zero.
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        }


        else if (!isWallRunning && !isSliding)
            MovePlayer();
        else
            SpeedControl();

        if (isWallRunning)
            WallRunMovement();

        CheckForNonWallRunnableCollision();


        Debug.Log("Current State: " + currentState + " | Velocity: " + rb.velocity.magnitude);
    }

    #endregion

    #region Input & Update Methods

    private void ProcessMouseLook(float dt)
    {
        // Horizontal (yaw) rotation is applied here
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * dt;
        transform.Rotate(Vector3.up * mouseX);
    }

    private void ProcessGroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
        if (grounded)
        {
            jumpCount = 0;
            isGroundPounding = false;
            if (isWallRunning)
                EndWallRun();
            if (isOnRail)
                ExitRail();
        }
    }

    private void ProcessFallRoll(float dt)
    {
        if (enableHighFallRoll)
        {
            if (!grounded && !fallStarted)
            {
                fallStarted = true;
                fallStartHeight = transform.position.y;
                fallStartTime = Time.time;
            }
            else if (grounded && fallStarted && !isRolling)
            {
                float fallDistance = fallStartHeight - transform.position.y;
                if (fallDistance >= highFallRollThreshold)
                    StartRoll();
                fallStarted = false;
            }
        }
    }

    private void ProcessMovementInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    private void ProcessJumpInput()
    {
        if (Input.GetKeyDown(jumpKey) && readyToJump &&
            (grounded || jumpCount < maxJumpCount || isWallRunning || isOnRail))
        {
            if (isOnRail)
                ExitRail();
            if (isCrouching && CanStand())
                StopCrouching();
            if (isWallRunning)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce((Vector3.up + currentWallNormal) * jumpForce, ForceMode.Impulse);
                EndWallRun();
                jumpCount = 1;
            }
            else
            {
                Jump();
                jumpCount++;
                if (grounded && Input.GetKey(sprintKey))
                    ResetDash();
            }
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void ProcessCrouchAndGroundPoundInput()
    {
        if (Input.GetKeyDown(crouchKey))
        {
            if (!grounded)
                GroundPound();
            else
            {
                if (Input.GetKey(sprintKey) && grounded && !isSliding)
                    Slide();
                else
                    StartCrouching();
            }
        }
        else if (!Input.GetKey(crouchKey))
        {
            if (isSliding)
                EndSlide();
            else if (isCrouching && CanStand())
                StopCrouching();
        }
    }

    private void ProcessDashInput()
    {
        if (Input.GetKeyDown(dashKey) && canDash && !isCrouching)
            Dash();
    }

    private void ProcessWallRunCheck(float dt)
    {
        if (!grounded && !isWallRunning && rb.velocity.y < 0)
            CheckForWallRun();
        else if (isWallRunning)
        {
            wallRunTimer += dt;
            if (wallRunTimer > maxWallRunTime || !IsWallStillValid())
                EndWallRun();
        }
    }

    private void SmoothHeadAndCollider(float dt)
    {
        head.localPosition = Vector3.Lerp(head.localPosition,
                                          isCrouching ? crouchHeadLocalPos : standHeadLocalPos,
                                          crouchTransitionSpeed * dt);
        float targetHeight = isCrouching ? crouchHeight : originalColliderHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchTransitionSpeed * dt);
        Vector3 targetCenter = originalColliderCenter;
        targetCenter.y -= (originalColliderHeight - targetHeight) / 2f;
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetCenter, crouchTransitionSpeed * dt);
    }

    private void UpdateCurrentWalkSpeed()
    {
        if (isOnRail && currentRail != null)
            currentWalkSpeed = currentRail.railSpeed;
        else if (isDashing)
            currentWalkSpeed = maxSpeed;
        else if (isSliding)
            currentWalkSpeed = slideSpeed;
        else if (isCrouching)
            currentWalkSpeed = crouchSpeed;
        else if (Input.GetKey(sprintKey) && grounded)
            currentWalkSpeed = sprintSpeed;
        else
            currentWalkSpeed = originalMoveSpeed;
    }

    private void UpdatePlayerState()
    {
        if (isRolling)
        {
            currentState = PlayerState.Rolling;
            return;
        }
        if (isOnRail)
            currentState = PlayerState.RailRiding;
        else if (isSliding)
            currentState = PlayerState.Sliding;
        else if (isWallRunning)
            currentState = PlayerState.WallRunning;
        else if (isDashing)
            currentState = PlayerState.Dashing;
        else if (!grounded)
            currentState = PlayerState.Jumping;
        else if (isCrouching)
            currentState = PlayerState.Crouching;
        else if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            currentState = (Input.GetKey(sprintKey) && grounded) ? PlayerState.Sprinting : PlayerState.Walking;
        else
            currentState = PlayerState.Idle;
    }

    private void UpdateCameraController()
    {
        if (playerCamera != null)
        {
            Camera_Controller camCtrl = playerCamera.GetComponent<Camera_Controller>();
            if (camCtrl != null)
            {
                camCtrl.SetPlayerState(currentState);
                camCtrl.SetRollFovOffset(CurrentRollFovOffset);
            }
        }
    }

    #endregion

    #region Movement & Utility Methods

    private float currentWalkSpeed; // current effective walk speed

    private void MovePlayer()
    {
        Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (grounded)
            rb.AddForce(moveDirection * currentWalkSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection * currentWalkSpeed * 10f * airMultiplier, ForceMode.Force);
        SpeedControl();
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > currentWalkSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentWalkSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void StartCrouching()
    {
        isCrouching = true;
    }

    private void StopCrouching()
    {
        isCrouching = false;
    }

    private bool CanStand()
    {
        float sphereRadius = capsuleCollider.radius * 0.9f;
        Vector3 spherePosition = transform.position + originalColliderCenter + Vector3.up * (originalColliderHeight * 0.5f);
        Collider[] hits = Physics.OverlapSphere(spherePosition, sphereRadius, whatIsGround);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject != gameObject)
                return false;
        }
        return true;
    }

    private void Dash()
    {
        canDash = false;
        isDashing = true;
        Vector3 dashDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
        Invoke(nameof(EndDash), dashDuration);
        Invoke(nameof(ResetDash), dashCooldown);
    }

    private void EndDash()
    {
        isDashing = false;
    }

    private void ResetDash()
    {
        canDash = true;
    }

    private void Slide()
    {
        isSliding = true;
        canDash = false;
        StartCrouching();
        Vector3 slideDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (slideDirection == Vector3.zero)
            slideDirection = transform.forward;
        rb.AddForce(slideDirection * dashForce, ForceMode.Impulse);
    }

    private void EndSlide()
    {
        isSliding = false;
        Invoke(nameof(ResetDash), dashCooldown);
    }

    public void EnterRail(Rail rail)
    {
        isOnRail = true;
        currentRail = rail;
        rb.useGravity = false;
        // Immediately snap the player’s vertical position to the rail’s surface plus offset.
        Vector3 railPoint = currentRail.GetClosestPointOnRail(rb.position);
        railPoint.y += railVerticalOffset;
        rb.position = new Vector3(rb.position.x, railPoint.y, rb.position.z);
    }


    public void ExitRail()
    {
        isOnRail = false;
        currentRail = null;
        rb.useGravity = true;
    }

    private void CheckForWallRun()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.right, out hit, wallRunRayDistance))
        {
            if (hit.normal.y < 0.2f && hit.collider.CompareTag("WallRun"))
            {
                StartWallRun(hit.normal);
                return;
            }
        }
        if (Physics.Raycast(transform.position, transform.right, out hit, wallRunRayDistance))
        {
            if (hit.normal.y < 0.2f && hit.collider.CompareTag("WallRun"))
            {
                StartWallRun(hit.normal);
                return;
            }
        }
    }

    private void StartWallRun(Vector3 wallNormal)
    {
        isWallRunning = true;
        wallRunTimer = 0f;
        currentWallNormal = wallNormal;
        rb.useGravity = false;
        Vector3 wallTangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallTangent, transform.forward) < 0)
            wallTangent = -wallTangent;
        rb.velocity = wallTangent * wallRunLaunchSpeed + new Vector3(0, rb.velocity.y, 0);
    }

    private void EndWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        rb.useGravity = true;
    }

    private bool IsWallStillValid()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -currentWallNormal, out hit, wallRunRayDistance, whatIsGround))
        {
            if (hit.collider.CompareTag("WallRun"))
            {
                if (hit.distance < 0.3f)
                    return false;
                return true;
            }
        }
        return false;
    }

    private void WallRunMovement()
    {
        float gravityMultiplier = Mathf.Lerp(0f, 1f, wallRunTimer / maxWallRunTime);
        rb.AddForce(Vector3.up * Physics.gravity.y * wallRunGravity * gravityMultiplier, ForceMode.Acceleration);
        Vector3 wallTangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallTangent, transform.forward) < 0)
            wallTangent = -wallTangent;
        Vector3 currentTangentVel = Vector3.Project(rb.velocity, wallTangent);
        if (currentTangentVel.magnitude < maxWallRunSpeed)
            rb.AddForce(wallTangent * wallRunAcceleration, ForceMode.Acceleration);
    }

    private void CheckForNonWallRunnableCollision()
    {
        if (!grounded && (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f))
        {
            if (transform.position.y > pushHeightThreshold && Time.time - lastPushTime >= pushCooldown)
            {
                float sphereRadius = capsuleCollider.radius * sphereRadiusMultiplier;
                Collider[] hits = Physics.OverlapSphere(transform.position, sphereRadius);
                foreach (Collider col in hits)
                {
                    if (col.gameObject == gameObject)
                        continue;
                    if (!col.CompareTag("WallRun"))
                    {
                        rb.AddForce(Vector3.down * pushForce, ForceMode.VelocityChange);
                        lastPushTime = Time.time;
                        break;
                    }
                }
            }
        }
    }

    private void GroundPound()
    {
        if (isGroundPounding)
            return;
        isGroundPounding = true;
        rb.AddForce(Vector3.down * groundPoundForce, ForceMode.VelocityChange);
        Debug.Log("Ground Pound triggered!");
    }

    public float CurrentRollFovOffset
    {
        get
        {
            if (isRolling)
            {
                float elapsed = Time.time - rollEffectStartTime;
                float t = Mathf.Clamp01(elapsed / rollDuration);
                return Mathf.Lerp(targetCameraFovIncrease, 0f, t);
            }
            return 0f;
        }
    }

    private void StartRoll()
    {
        if (isRolling)
            return;
        float fallDuration = Time.time - fallStartTime;
        targetCameraFovIncrease = Mathf.Clamp(fallDuration * fovMultiplier, 0f, maxFovIncrease);
        targetCameraRollAngle = Mathf.Clamp(fallDuration * rollMultiplier, 0f, maxCameraRollAngle);
        isRolling = true;
        currentState = PlayerState.Rolling;
        rollEffectStartTime = Time.time;
        rb.AddForce(transform.forward * rollForce, ForceMode.Impulse);
        Invoke(nameof(EndRoll), rollDuration);
    }

    private void EndRoll()
    {
        isRolling = false;
    }

    #endregion
}
