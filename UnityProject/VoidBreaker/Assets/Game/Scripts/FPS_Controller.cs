using UnityEngine;

public enum PlayerState
{
    Idle,
    Walking,
    Crouching,
    Dashing,
    Jumping
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

        // Lock and hide the cursor for FPS control
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
            // Reset jump count when touching the ground.
            jumpCount = 0;
        }

        // --- Movement Input ---
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // --- Jump Input (with Double Jump) ---
        // Allow jump if the player is on the ground or has remaining jumps.
        if (Input.GetKeyDown(jumpKey) && readyToJump && (grounded || jumpCount < maxJumpCount))
        {
            // If crouched and jump is pressed, try to stand up first if there's room overhead.
            if (isCrouching && CanStand())
            {
                StopCrouching();
            }
            readyToJump = false;
            Jump();
            jumpCount++;
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // --- Crouch Input ---
        if (Input.GetKey(crouchKey))
        {
            StartCrouching();
        }
        else
        {
            // Only attempt to stand if currently crouching and there’s room overhead.
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

        // Update the state machine (lowest-priority states last).
        UpdatePlayerState();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        // Calculate movement direction based on the player's forward/right (horizontal only).
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
    /// Checks if there is sufficient room above the player to return to standing height.
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
        // Determine dash direction based on input (or default to forward if no input).
        Vector3 dashDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward;
        rb.AddForce(dashDirection * dashForce, ForceMode.Impulse);
        Invoke(nameof(EndDash), dashDuration);
        Invoke(nameof(ResetDash), dashCooldown);
    }

    /// <summary>
    /// Ends the dash effect.
    /// </summary>
    private void EndDash()
    {
        isDashing = false;
    }

    /// <summary>
    /// Resets the dash so the player can dash again.
    /// </summary>
    private void ResetDash()
    {
        canDash = true;
    }

    /// <summary>
    /// Updates the player's state based on movement and input.
    /// </summary>
    private void UpdatePlayerState()
    {
        if (isDashing)
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
