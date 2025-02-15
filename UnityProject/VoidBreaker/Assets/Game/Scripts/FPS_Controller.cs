using UnityEngine;

public enum PlayerState
{
    Idle,
    Walking,
    Crouching,
    Dashing,
    Jumping,
    WallRunning  // New state for wall running
}

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class FPS_Controller : MonoBehaviour
{
    [Header("Hierarchy References")]
    [Tooltip("Drag the 'Head' transform here (child of the Player).")]
    public Transform head;

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
    public KeyCode crouchKey = KeyCode.LeftControl; // change this key as desired
    public KeyCode dashKey = KeyCode.LeftShift;     // dash key

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

    // Enum state machine: exposes the current player action.
    private PlayerState currentState = PlayerState.Idle;
    public PlayerState CurrentState { get { return currentState; } }

    // Internal references
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Input
    private float horizontalInput;
    private float verticalInput;

    // Original values
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private float originalMoveSpeed;
    private Vector3 standHeadLocalPos;
    private Vector3 crouchHeadLocalPos;

    // Crouch state
    private bool isCrouching = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;  // Prevent physics from rotating the player

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            Debug.LogError("No CapsuleCollider found on the player!");
        }
        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
        originalMoveSpeed = moveSpeed;
        currentWalkSpeed = originalMoveSpeed;

        // Store head's original local position and compute its crouched position.
        standHeadLocalPos = head.localPosition;
        crouchHeadLocalPos = new Vector3(standHeadLocalPos.x, standHeadLocalPos.y - crouchCameraHeight, standHeadLocalPos.z);

        // Lock and hide the cursor for FPS control.
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
        head.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // --- Ground Check ---
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);
        if (grounded)
        {
            // Reset jump count and end wall run when on ground.
            jumpCount = 0;
            if (isWallRunning)
                EndWallRun();
        }

        // --- Movement Input ---
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // --- Jump Input (Double Jump & Wall Jump) ---
        if (Input.GetKeyDown(jumpKey) && readyToJump && (grounded || jumpCount < maxJumpCount || isWallRunning))
        {
            if (isCrouching && CanStand())
            {
                StopCrouching();
            }
            // If wall running, perform a wall jump that pushes off the wall.
            if (isWallRunning)
            {
                //print a message to the console
                Debug.Log("Performing wall jump: " + currentWallNormal);
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce((Vector3.up + currentWallNormal) * jumpForce, ForceMode.Impulse);
                EndWallRun();
                jumpCount = 1; // Count as one jump used.
            }
            else
            {
                Jump();
                jumpCount++;
            }
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // --- Crouch Input ---
        if (Input.GetKey(crouchKey))
        {
            StartCrouching();
        }
        else
        {
            if (isCrouching && CanStand())
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

        // Smooth head (camera) position transition.
        head.localPosition = Vector3.Lerp(head.localPosition, isCrouching ? crouchHeadLocalPos : standHeadLocalPos, crouchTransitionSpeed * Time.deltaTime);

        // Smooth collider transition.
        float targetHeight = isCrouching ? crouchHeight : originalColliderHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        Vector3 targetCenter = originalColliderCenter;
        targetCenter.y -= (originalColliderHeight - targetHeight) / 2;
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetCenter, crouchTransitionSpeed * Time.deltaTime);

        // Update current walk speed:
        // Dash overrides other states; otherwise, use crouch speed if crouching, or normal speed.
        currentWalkSpeed = isDashing ? maxSpeed : (isCrouching ? crouchSpeed : originalMoveSpeed);

        // Update the state machine.
        UpdatePlayerState();
    }

    private void FixedUpdate()
    {
        // If not wall running, process normal movement.
        if (!isWallRunning)
        {
            MovePlayer();
        }
        else
        {
            // During wall run, ignore player input to preserve the launched momentum.
            SpeedControl();
        }

        // While wall running, apply wall run behavior.
        if (isWallRunning)
        {
            WallRunMovement();
        }
    }

    private void MovePlayer()
    {
        // Calculate movement direction based on player's forward/right (horizontal only).
        Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

        if (grounded)
            rb.AddForce(moveDirection * currentWalkSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDirection * currentWalkSpeed * 10f * airMultiplier, ForceMode.Force);

        SpeedControl();
    }

    private void SpeedControl()
    {
        // Limit horizontal speed without affecting vertical velocity.
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > currentWalkSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentWalkSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // Reset vertical velocity and apply jump impulse.
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

    /// <summary>
    /// Checks if there is room above the player to stand up.
    /// </summary>
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

    /// <summary>
    /// Initiates a dash in the current movement direction.
    /// </summary>
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
    /// Checks for a wall on either side to initiate a wall run.
    /// </summary>
    private void CheckForWallRun()
    {
        RaycastHit hit;
        // Check left side.
        if (Physics.Raycast(transform.position, -transform.right, out hit, wallRunRayDistance))
        {
            if (hit.normal.y < 0.2f) // nearly vertical wall
            {
                StartWallRun(hit.normal);
                return;
            }
        }
        // Check right side.
        if (Physics.Raycast(transform.position, transform.right, out hit, wallRunRayDistance))
        {
            if (hit.normal.y < 0.2f)
            {
                StartWallRun(hit.normal);
                return;
            }
        }
    }

    /// <summary>
    /// Begins wall running given a valid wall normal.
    /// Immediately launches the player along the wall.
    /// </summary>
    private void StartWallRun(Vector3 wallNormal)
    {
        isWallRunning = true;
        wallRunTimer = 0f;
        currentWallNormal = wallNormal;
        rb.useGravity = false;

        // Calculate the wall tangent (direction along the wall).
        Vector3 wallTangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallTangent, transform.forward) < 0)
            wallTangent = -wallTangent;

        // Launch the player along the wall.
        rb.velocity = wallTangent * wallRunLaunchSpeed + new Vector3(0, rb.velocity.y, 0);
    }

    /// <summary>
    /// Ends the wall run, restoring normal gravity.
    /// </summary>
    private void EndWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        rb.useGravity = true;
    }

    /// <summary>
    /// Checks if the wall we are running on is still valid.
    /// </summary>
    private bool IsWallStillValid()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -currentWallNormal, out hit, wallRunRayDistance))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Applies wall run behavior by preserving momentum, gradually increasing speed, and ramping up gravity.
    /// </summary>
    private void WallRunMovement()
    {
        // Ramp up gravity over the wall run time so the player starts to descend.
        float gravityMultiplier = Mathf.Lerp(0f, 1f, wallRunTimer / maxWallRunTime);
        rb.AddForce(Vector3.up * Physics.gravity.y * wallRunGravity * gravityMultiplier, ForceMode.Acceleration);

        // Increase speed along the wall.
        Vector3 wallTangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallTangent, transform.forward) < 0)
            wallTangent = -wallTangent;
        Vector3 currentTangentVel = Vector3.Project(rb.velocity, wallTangent);
        if (currentTangentVel.magnitude < maxWallRunSpeed)
        {
            rb.AddForce(wallTangent * wallRunAcceleration, ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Updates the player's state based on movement and input.
    /// </summary>
    private void UpdatePlayerState()
    {
        if (isWallRunning)
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
            currentState = PlayerState.Walking;
        }
        else
        {
            currentState = PlayerState.Idle;
        }
    }
}
