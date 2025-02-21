using UnityEngine;

public enum AIMovementState
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
public class AI_MovementController : MonoBehaviour
{
    #region Public Fields


    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float groundDrag = 6f;
    public float jumpForce = 5f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.5f;
    public int maxJumpCount = 2;

    [Header("Speeds & Key Movement")]
    public float sprintSpeed = 8f;
    public float crouchSpeed = 3f;
    public float dashForce = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;
    public float slideSpeed = 12f;

    [Header("Collider & Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 8f;
    public float crouchCameraHeight = 0.5f;

    [Header("Wall Run Settings")]
    public float wallRunLaunchSpeed = 10f;
    public float wallRunAcceleration = 5f;
    public float maxWallRunSpeed = 20f;
    public float wallRunGravity = 0.3f;
    public float maxWallRunTime = 2f;
    public float wallRunRayDistance = 1.0f;

    [Header("Rail Riding")]
    public float railVerticalOffset = 1f;

    [Header("High Fall Roll")]
    public bool enableHighFallRoll = true;
    public float highFallRollThreshold = 5f;
    public float rollDuration = 0.5f;
    public float rollForce = 5f;

    [Header("Non-Wall Runnable Push")]
    public float pushForce = 10f;
    public float sphereRadiusMultiplier = 1.2f;
    public float pushHeightThreshold = 2f;
    public float pushCooldown = 1f;

    [Header("Ground Pound")]
    public float groundPoundForce = 20f;

    [Header("Performance")]
    public float maxDeltaTime = 0.016f; // ~60 fps

    [HideInInspector] public AIMovementState currentState = AIMovementState.Idle;

    #endregion

    #region Private Fields

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Movement inputs driven by AI
    private float horizontalInput;
    private float verticalInput;
    private bool wantToJump;
    private bool wantToCrouch;
    private bool wantToSprint;
    private bool wantToDash;

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

    // For storing current walk speed
    private float currentWalkSpeed;

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

        // If you had a "head" transform for the camera, adapt as needed:
        standHeadLocalPos = Vector3.zero;
        crouchHeadLocalPos = new Vector3(0, -crouchCameraHeight, 0);


        readyToJump = true;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);

        ProcessGroundCheck();
        ProcessFallRoll(dt);
        ProcessMovementLogic();  // uses AI inputs
        ProcessJumpLogic();
        ProcessCrouchLogic();
        ProcessDashLogic();
        ProcessWallRunCheck(dt);
        SmoothColliderAndCamera(dt);
        UpdateCurrentWalkSpeed();
        UpdateState();
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
            HandleRailMovement();
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
            WallRunMovement();

        CheckForNonWallRunnableCollision();
    }

    #endregion

    #region Public AI Control Methods

    /// <summary>
    /// Call this every frame from your AI script to set movement inputs.
    /// horizontal and vertical range: [-1..1].
    /// </summary>
    public void SetMovementInput(float horizontal, float vertical, bool sprint, bool crouch, bool jump, bool dash)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
        wantToSprint = sprint;
        wantToCrouch = crouch;
        wantToJump = jump;
        wantToDash = dash;
    }

    /// <summary>
    /// If your AI wants to forcibly jump right now, call this method directly.
    /// </summary>
    public void AIJump()
    {
        if (readyToJump && (grounded || jumpCount < maxJumpCount || isWallRunning || isOnRail))
        {
            if (isOnRail) ExitRail();
            if (isCrouching && CanStand()) StopCrouching();

            if (isWallRunning)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce((Vector3.up + currentWallNormal) * jumpForce, ForceMode.Impulse);
                EndWallRun();
                jumpCount = 1;
            }
            else
            {
                PerformJump();
                jumpCount++;
            }
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    /// <summary>
    /// If your AI wants to forcibly dash right now, call this method directly.
    /// </summary>
    public void AIDash()
    {
        if (canDash && !isCrouching)
            StartDash();
    }

    /// <summary>
    /// If your AI wants to forcibly crouch, call this method.
    /// </summary>
    public void AICrouch(bool crouch)
    {
        if (crouch) StartCrouching();
        else if (isCrouching && CanStand()) StopCrouching();
    }

    /// <summary>
    /// If your AI wants to forcibly ground pound (when airborne), call this.
    /// </summary>
    public void AIGroundPound()
    {
        if (!grounded && !isGroundPounding)
        {
            isGroundPounding = true;
            rb.AddForce(Vector3.down * groundPoundForce, ForceMode.VelocityChange);
        }
    }

    /// <summary>
    /// Teleport or set position for the AI (e.g., from a GOAP Action).
    /// </summary>
    public void AISetPosition(Vector3 position)
    {
        rb.position = position;
        rb.velocity = Vector3.zero;
    }

    #endregion

    #region Internal Logic Methods

    private void ProcessGroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down,
            playerHeight * 0.5f + 0.3f, whatIsGround);
        if (grounded)
        {
            jumpCount = 0;
            isGroundPounding = false;
            if (isWallRunning) EndWallRun();
            if (isOnRail) ExitRail();
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

    private void ProcessMovementLogic()
    {
        // Movement input is set by AI via SetMovementInput()
        // We'll just keep them for horizontalInput, verticalInput, etc.
    }

    private void ProcessJumpLogic()
    {
        if (wantToJump)
        {
            AIJump();
            // Reset the flag so we don't spam jump
            wantToJump = false;
        }
    }

    private void ProcessCrouchLogic()
    {
        // If AI wants to crouch
        if (wantToCrouch)
        {
            // If airborne => ground pound
            if (!grounded) AIGroundPound();
            else
            {
                // Possibly do a slide if sprinting
                if (wantToSprint && grounded && !isSliding)
                    StartSlide();
                else
                    StartCrouching();
            }
        }
        else
        {
            // If sliding, end slide
            if (isSliding) EndSlide();
            // else if crouching, stand up
            else if (isCrouching && CanStand()) StopCrouching();
        }
        // Reset the crouch input
        wantToCrouch = false;
    }

    private void ProcessDashLogic()
    {
        if (wantToDash)
        {
            AIDash();
            wantToDash = false;
        }
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

    private void SmoothColliderAndCamera(float dt)
    {
        // Smoothly adjust capsule height
        float targetHeight = isCrouching ? crouchHeight : originalColliderHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchTransitionSpeed * dt);

        Vector3 targetCenter = originalColliderCenter;
        targetCenter.y -= (originalColliderHeight - targetHeight) / 2f;
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetCenter, crouchTransitionSpeed * dt);

        // If you had a "head" transform, you could move it up/down. If you have a camera child, do so here:
        // (Skipping in this example unless you have a head transform to move)
    }

    private void UpdateCurrentWalkSpeed()
    {
        if (isOnRail && currentRail != null)
            currentWalkSpeed = currentRail.railSpeed;
        else if (isDashing)
            currentWalkSpeed = dashForce;  // or maxSpeed
        else if (isSliding)
            currentWalkSpeed = slideSpeed;
        else if (isCrouching)
            currentWalkSpeed = crouchSpeed;
        else if (wantToSprint && grounded)
            currentWalkSpeed = sprintSpeed;
        else
            currentWalkSpeed = moveSpeed;
    }

    private void UpdateState()
    {
        if (isRolling)
        {
            currentState = AIMovementState.Rolling;
            return;
        }
        if (isOnRail)
            currentState = AIMovementState.RailRiding;
        else if (isSliding)
            currentState = AIMovementState.Sliding;
        else if (isWallRunning)
            currentState = AIMovementState.WallRunning;
        else if (isDashing)
            currentState = AIMovementState.Dashing;
        else if (!grounded)
            currentState = AIMovementState.Jumping;
        else if (isCrouching)
            currentState = AIMovementState.Crouching;
        else if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            currentState = (wantToSprint && grounded) ? AIMovementState.Sprinting : AIMovementState.Walking;
        else
            currentState = AIMovementState.Idle;
    }


    private void MovePlayer()
    {
        Vector3 moveDir = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (grounded)
            rb.AddForce(moveDir * currentWalkSpeed * 10f, ForceMode.Force);
        else
            rb.AddForce(moveDir * currentWalkSpeed * 10f * airMultiplier, ForceMode.Force);

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

    private void PerformJump()
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
        Vector3 spherePos = transform.position + originalColliderCenter + Vector3.up * (originalColliderHeight * 0.5f);
        Collider[] hits = Physics.OverlapSphere(spherePos, sphereRadius, whatIsGround);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject) return false;
        }
        return true;
    }

    private void StartDash()
    {
        canDash = false;
        isDashing = true;
        Vector3 dashDir = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (dashDir == Vector3.zero)
            dashDir = transform.forward;
        rb.AddForce(dashDir * dashForce, ForceMode.Impulse);
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

    private void StartSlide()
    {
        isSliding = true;
        canDash = false;
        StartCrouching();
        Vector3 slideDir = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (slideDir == Vector3.zero)
            slideDir = transform.forward;
        rb.AddForce(slideDir * dashForce, ForceMode.Impulse);
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

    private void HandleRailMovement()
    {
        Vector3 railPoint = currentRail.GetClosestPointOnRail(rb.position);
        railPoint.y += railVerticalOffset;

        Vector3 newPos = rb.position;
        newPos.x = Mathf.Lerp(rb.position.x, railPoint.x, 0.2f);
        newPos.z = Mathf.Lerp(rb.position.z, railPoint.z, 0.2f);
        newPos.y = railPoint.y;
        rb.MovePosition(newPos);

        Vector3 railDir = currentRail.GetRailDirection();
        railDir = Vector3.ProjectOnPlane(railDir, Vector3.up).normalized;

        if (Vector3.Dot(transform.forward, railDir) < 0f)
            railDir = -railDir;

        rb.velocity = railDir * currentRail.railSpeed;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
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
                if (hit.distance < 0.3f) return false;
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
                    if (col.gameObject == gameObject) continue;
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

    private void StartRoll()
    {
        if (isRolling) return;
        float fallDuration = Time.time - fallStartTime;
        isRolling = true;
        currentState = AIMovementState.Rolling;
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
