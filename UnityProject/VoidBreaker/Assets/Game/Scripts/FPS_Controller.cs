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
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

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
    private bool readyToJump = true;

    [Header("Double Jump Settings")]
    [Tooltip("Maximum number of jumps (including the initial jump).")]
    public int maxJumpCount = 2;
    private int jumpCount = 0;

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
    private bool grounded;

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
    [Tooltip("Current effective walking speed.")]
    public float currentWalkSpeed;
    [Tooltip("Maximum speed achievable during a dash.")]
    public float maxSpeed = 10f;

    [Header("Dash Settings")]
    [Tooltip("Force applied when dashing.")]
    public float dashForce = 15f;
    [Tooltip("Duration of the dash effect.")]
    public float dashDuration = 0.2f;
    [Tooltip("Cooldown before you can dash again.")]
    public float dashCooldown = 2f;
    private bool canDash = true;
    public bool isDashing = false;

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
    private float wallRunTimer = 0f;
    private bool isWallRunning = false;
    private Vector3 currentWallNormal;

    [Header("Slide Settings")]
    [Tooltip("Slide is essentially a dash that forces crouch height.")]
    public float slideSpeed = 12f;
    private bool isSliding = false;

    [Header("Rail Riding Settings")]
    [Tooltip("Reference to the Rail object when riding a rail.")]
    private Rail currentRail = null;
    [Tooltip("Offset to position the player on top of the rail.")]
    public float railVerticalOffset = 1f;
    private bool isOnRail = false;

    [Header("High Fall Roll Settings")]
    [Tooltip("Enable or disable the high fall roll feature.")]
    public bool enableHighFallRoll = true;
    [Tooltip("Minimum fall distance (in meters) needed to trigger a roll.")]
    public float highFallRollThreshold = 5f;
    [Tooltip("Duration of the roll in seconds.")]
    public float rollDuration = 0.5f;
    [Tooltip("Forward impulse applied when rolling.")]
    public float rollForce = 5f;
    // Internal tracking for fall.
    private float fallStartHeight;
    private float fallStartTime;
    private bool fallStarted = false;

    // For camera roll effect.
    private float rollEffectStartTime;
    private float targetCameraFovIncrease;
    private float targetCameraRollAngle;
    private bool isRolling = false;

    [Header("Non-Wall Runnable Push Settings")]
    [Tooltip("Force to push the player downward if colliding with a non-wall runnable object while in air and pressing movement.")]
    public float pushForce = 10f;
    [Tooltip("Multiplier to expand the sphere radius for collision detection.")]
    public float sphereRadiusMultiplier = 1.2f;
    [Tooltip("Height threshold (player's Y position) above which the push is applied.")]
    public float pushHeightThreshold = 2f;
    [Tooltip("Minimum time (in seconds) between push events.")]
    public float pushCooldown = 1f;
    private float lastPushTime = 0f;

    [Header("Ground Pound Settings")]
    [Tooltip("Downward force applied when ground pounding.")]
    public float groundPoundForce = 20f;
    // Tracks whether a ground pound is currently active.
    private bool isGroundPounding = false;

    // Enum state machine.
    private PlayerState currentState = PlayerState.Idle;
    public PlayerState CurrentState { get { return currentState; } }

    // Internal references.
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Input.
    private float horizontalInput;
    private float verticalInput;

    // Original values.
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private float originalMoveSpeed;
    private Vector3 standHeadLocalPos;
    private Vector3 crouchHeadLocalPos;

    // Crouch state.
    private bool isCrouching = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
            Debug.LogError("No CapsuleCollider found on the player!");

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
        originalMoveSpeed = moveSpeed;
        currentWalkSpeed = originalMoveSpeed;

        standHeadLocalPos = head.localPosition;
        crouchHeadLocalPos = new Vector3(standHeadLocalPos.x, standHeadLocalPos.y - crouchCameraHeight, standHeadLocalPos.z);

        // If no camera is assigned, try to get one from the head.
        if (playerCamera == null)
            playerCamera = head.GetComponent<Camera>();

        // Set camera to its base FOV.
        if (playerCamera != null)
            playerCamera.fieldOfView = baseFov;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        readyToJump = true;
    }

    private void Update()
    {
        // --- Mouse Look ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // --- Ground Check ---
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
        if (grounded)
        {
            jumpCount = 0;
            isGroundPounding = false; // Reset ground pound when landed.
            if (isWallRunning)
                EndWallRun();
            if (isOnRail)
                ExitRail();
        }

        // --- High Fall Roll Tracking ---
        if (enableHighFallRoll)
        {
            // When leaving the ground, start tracking the fall.
            if (!grounded && !fallStarted)
            {
                fallStarted = true;
                fallStartHeight = transform.position.y;
                fallStartTime = Time.time;
            }
            // When landing, check the fall distance.
            else if (grounded && fallStarted && !isRolling)
            {
                float fallDistance = fallStartHeight - transform.position.y;
                if (fallDistance >= highFallRollThreshold)
                {
                    StartRoll();
                }
                fallStarted = false; // Reset fall tracking.
            }
        }

        // --- Movement Input ---
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // --- Jump Input ---
        if (Input.GetKeyDown(jumpKey) && readyToJump && (grounded || jumpCount < maxJumpCount || isWallRunning || isOnRail))
        {
            if (isOnRail)
            {
                ExitRail();
            }
            if (isCrouching && CanStand())
            {
                StopCrouching();
            }
            if (isWallRunning)
            {
                Debug.Log("Performing wall jump: " + currentWallNormal);
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
                {
                    ResetDash(); // Reset dash cooldown when sprinting and jumping.
                }
            }
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // --- Crouch / Ground Pound Input ---
        if (Input.GetKeyDown(crouchKey))
        {
            if (!grounded)
            {
                // If in air and CTRL is pressed, perform a ground pound.
                GroundPound();
            }
            else
            {
                // If grounded, trigger slide if sprinting; otherwise, start crouching.
                if (Input.GetKey(sprintKey) && grounded && !isSliding)
                {
                    Slide();
                }
                else
                {
                    StartCrouching();
                }
            }
        }
        else if (!Input.GetKey(crouchKey))
        {
            if (isSliding)
            {
                EndSlide();
            }
            else if (isCrouching && CanStand())
            {
                StopCrouching();
            }
        }

        // --- Dash Input ---
        if (Input.GetKeyDown(dashKey) && canDash && !isCrouching)
        {
            Dash();
        }

        // --- Wall Run Check ---
        if (!grounded && !isWallRunning && rb.velocity.y < 0)
        {
            CheckForWallRun();
        }
        else if (isWallRunning)
        {
            wallRunTimer += Time.deltaTime;
            if (wallRunTimer > maxWallRunTime || !IsWallStillValid())
            {
                EndWallRun();
            }
        }

        // --- Smooth Head and Collider Transition ---
        head.localPosition = Vector3.Lerp(head.localPosition, isCrouching ? crouchHeadLocalPos : standHeadLocalPos, crouchTransitionSpeed * Time.deltaTime);
        float targetHeight = isCrouching ? crouchHeight : originalColliderHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        Vector3 targetCenter = originalColliderCenter;
        targetCenter.y -= (originalColliderHeight - targetHeight) / 2;
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);

        // --- Update Current Walk Speed ---
        if (isOnRail && currentRail != null)
        {
            currentWalkSpeed = currentRail.railSpeed; // Use rail speed.
        }
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

        // --- Update Camera Roll & FOV if Rolling ---
        if (isRolling)
        {
            float elapsedRoll = Time.time - rollEffectStartTime;
            float t = Mathf.Clamp01(elapsedRoll / rollDuration);
            // Lerp from the target (maximum) values back to zero.
            float currentRollAngle = Mathf.Lerp(targetCameraRollAngle, 0, t);
            float currentFovIncrease = Mathf.Lerp(targetCameraFovIncrease, 0, t);
            if (playerCamera != null)
                playerCamera.fieldOfView = baseFov + currentFovIncrease;
            // Combine pitch (xRotation) with roll (z rotation).
            head.localRotation = Quaternion.Euler(xRotation, 0, currentRollAngle);
        }
        else
        {
            Debug.Log(xRotation);
            // Normal head rotation and FOV when not rolling.
            head.localRotation = Quaternion.Euler(xRotation, 0, 0);
            if (playerCamera != null)
                playerCamera.fieldOfView = baseFov;
        }

        UpdatePlayerState();
    }

    private void FixedUpdate()
    {
        // --- Disable normal movement while rolling ---
        if (isRolling)
        {
            SpeedControl();
            return;
        }

        if (isOnRail && currentRail != null)
        {
            // Stick the player to the top of the rail.
            Vector3 targetPos = currentRail.GetClosestPointOnRail(rb.position) + Vector3.up * railVerticalOffset;
            rb.position = Vector3.Lerp(rb.position, targetPos, 0.2f);
            Vector3 railDir = currentRail.GetRailDirection();
            rb.velocity = new Vector3(railDir.x * currentRail.railSpeed, rb.velocity.y, railDir.z * currentRail.railSpeed);
        }
        else if (!isWallRunning && !isSliding)
        {
            MovePlayer();
        }
        else
        {
            SpeedControl();
        }

        if (isWallRunning)
        {
            WallRunMovement();
        }

        // --- Check for collisions with non-wall runnable objects ---
        CheckForNonWallRunnableCollision();
    }

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

    /// <summary>
    /// Performs a slide by using the dash impulse while forcing crouch height.
    /// The slide persists until the player releases the crouch key.
    /// </summary>
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

    // Rail riding integration.
    public void EnterRail(Rail rail)
    {
        isOnRail = true;
        currentRail = rail;
        rb.useGravity = false;
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
        // Check left side.
        if (Physics.Raycast(transform.position, -transform.right, out hit, wallRunRayDistance))
        {
            if (hit.normal.y < 0.2f && hit.collider.CompareTag("WallRun"))
            {
                StartWallRun(hit.normal);
                return;
            }
        }
        // Check right side.
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
        // Cast a ray from the player's position in the opposite direction of the wall's normal.
        if (Physics.Raycast(transform.position, -currentWallNormal, out hit, wallRunRayDistance, whatIsGround))
        {
            // Only consider walls tagged "WallRun".
            if (hit.collider.CompareTag("WallRun"))
            {
                // If the distance to the wall is too short (e.g. less than 0.3f), cancel the wall run.
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

    private void UpdatePlayerState()
    {
        // Rolling takes precedence over other states.
        if (isRolling)
        {
            currentState = PlayerState.Rolling;
            Debug.Log("Current State: " + currentState);
            return;
        }
        if (isOnRail)
        {
            currentState = PlayerState.RailRiding;
        }
        else if (isSliding)
        {
            currentState = PlayerState.Sliding;
        }
        else if (isWallRunning)
        {
            currentState = PlayerState.WallRunning;
        }
        else if (isDashing)
        {
            currentState = PlayerState.Dashing;
        }
        else if (!grounded)
        {
            currentState = PlayerState.Jumping;
        }
        else if (isCrouching)
        {
            currentState = PlayerState.Crouching;
        }
        else if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
        {
            if (Input.GetKey(sprintKey) && grounded)
                currentState = PlayerState.Sprinting;
            else
                currentState = PlayerState.Walking;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
        Debug.Log("Current State: " + currentState);
    }

    /// <summary>
    /// Initiates a roll after a high fall.
    /// Also computes camera roll and FOV boost based on how long the fall lasted.
    /// </summary>
    private void StartRoll()
    {
        if (isRolling)
            return;

        // Calculate how long the player was falling.
        float fallDuration = Time.time - fallStartTime;
        // Compute the maximum FOV increase and roll angle based on fall duration.
        targetCameraFovIncrease = Mathf.Clamp(fallDuration * fovMultiplier, 0, maxFovIncrease);
        targetCameraRollAngle = Mathf.Clamp(fallDuration * rollMultiplier, 0, maxCameraRollAngle);

        isRolling = true;
        currentState = PlayerState.Rolling;
        rollEffectStartTime = Time.time;
        // Apply a forward impulse for extra roll momentum.
        rb.AddForce(transform.forward * rollForce, ForceMode.Impulse);
        // End the roll after the designated duration.
        Invoke(nameof(EndRoll), rollDuration);
    }

    private void EndRoll()
    {
        isRolling = false;
        // After rolling, the state will update based on inputs/grounded status.
    }

    /// <summary>
    /// If the player is in air and pressing movement keys, perform an OverlapSphere check.
    /// If a collider is found that is NOT tagged "WallRun", push the player downward.
    /// The push is only applied if the player's Y position is above a threshold and if a cooldown period has passed.
    /// </summary>
    private void CheckForNonWallRunnableCollision()
    {
        if (!grounded && (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f))
        {
            // Only push if the player's height is above the threshold and the cooldown has elapsed.
            if (transform.position.y > pushHeightThreshold && Time.time - lastPushTime >= pushCooldown)
            {
                float sphereRadius = capsuleCollider.radius * sphereRadiusMultiplier;
                Collider[] hits = Physics.OverlapSphere(transform.position, sphereRadius);
                foreach (Collider col in hits)
                {
                    // Skip self.
                    if (col.gameObject == gameObject)
                        continue;
                    // If the collider isn't tagged as "WallRun", apply a downward force.
                    if (!col.CompareTag("WallRun"))
                    {
                        rb.AddForce(Vector3.down * pushForce, ForceMode.VelocityChange);
                        lastPushTime = Time.time;
                        break; // Only need to push once.
                    }
                }
            }
        }
    }

    /// <summary>
    /// Performs a ground pound when the player is in air and presses the crouch key.
    /// Applies a strong downward force. This is only triggered when airborne.
    /// </summary>
    private void GroundPound()
    {
        if (isGroundPounding)
            return;

        isGroundPounding = true;
        rb.AddForce(Vector3.down * groundPoundForce, ForceMode.VelocityChange);
        Debug.Log("Ground Pound triggered!");
    }
}
