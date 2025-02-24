using UnityEngine;

public enum AIPlayerState
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
public class AI_Movement_Controller : MonoBehaviour
{
    #region Public Fields

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float groundDrag = 6f;
    public float jumpForce = 5f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.5f;
    public int maxJumpCount = 2;

    [Header("Sprinting Settings")]
    public float sprintSpeed = 8f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;
    public float crouchTransitionSpeed = 8f;
    public float crouchCameraHeight = 0.5f;

    [Header("Dash Settings")]
    public float dashForce = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;
    public float maxSpeed = 10f;   // top dash speed

    [Header("Wall Run Settings")]
    public float wallRunLaunchSpeed = 10f;
    public float wallRunAcceleration = 5f;
    public float maxWallRunSpeed = 20f;
    public float wallRunGravity = 0.3f;
    public float maxWallRunTime = 2f;
    public float wallRunRayDistance = 1.0f;

    [Header("Slide Settings")]
    public float slideSpeed = 12f;

    [Header("Rail Riding Settings")]
    public float railVerticalOffset = 1f;

    [Header("High Fall Roll Settings")]
    public bool enableHighFallRoll = true;
    public float highFallRollThreshold = 5f;
    public float rollDuration = 0.5f;
    public float rollForce = 5f;

    [Header("Non-Wall Runnable Push Settings")]
    public float pushForce = 10f;
    public float sphereRadiusMultiplier = 1.2f;
    public float pushHeightThreshold = 2f;
    public float pushCooldown = 1f;

    [Header("Ground Pound Settings")]
    public float groundPoundForce = 20f;

    [Header("Performance Settings")]
    public float maxDeltaTime = 0.016f; // ~60 fps

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;

    #endregion

    #region Private Fields

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // AI-driven input fields
    private float horizontalInput;
    private float verticalInput;
    private bool wantSprint;
    private bool wantCrouch;
    private bool wantJump;
    private bool wantDash;
    // For AI rotation (yaw)
    private float aiLookYaw;

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

    private AIPlayerState currentState = AIPlayerState.Idle;
    public AIPlayerState CurrentState { get { return currentState; } }

    private float currentWalkSpeed;

    #endregion

    #region AI Input Methods

    /// <summary>
    /// Call this from your AI script every frame to set the movement & action inputs.
    /// </summary>
    /// <param name="horizontal">[-1..1] strafe</param>
    /// <param name="vertical">[-1..1] forward/back</param>
    /// <param name="wantSprint">Should the AI attempt to sprint?</param>
    /// <param name="wantCrouch">Should the AI attempt to crouch/slide/groundpound?</param>
    /// <param name="wantJump">Should the AI attempt to jump?</param>
    /// <param name="wantDash">Should the AI attempt to dash?</param>
    /// <param name="lookYaw">Desired yaw rotation in degrees this frame.</param>
    public void SetAIInput(float horizontal, float vertical,
                           bool wantSprint, bool wantCrouch,
                           bool wantJump, bool wantDash,
                           float lookYaw)
    {
        this.horizontalInput = horizontal;
        this.verticalInput = vertical;
        this.wantSprint = wantSprint;
        this.wantCrouch = wantCrouch;
        this.wantJump = wantJump;
        this.wantDash = wantDash;
        this.aiLookYaw = lookYaw;
    }

    #endregion

    #region Unity Methods

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
            Debug.LogError("No CapsuleCollider found on the AI character!");

        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
        originalMoveSpeed = moveSpeed;


        currentState = AIPlayerState.Idle;
        readyToJump = true;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);

        // 1) Rotate horizontally according to AI yaw
        transform.Rotate(Vector3.up * aiLookYaw);

        // 2) Check ground, fall logic
        ProcessGroundCheck();
        ProcessFallRoll(dt);

        // 3) Movement logic (no direct Input calls—AI sets the variables)
        ProcessJumpInput();
        ProcessCrouchAndGroundPoundInput();
        ProcessDashInput();
        ProcessWallRunCheck(dt);
        SmoothHeadAndCollider(dt);
        UpdateCurrentWalkSpeed();
        UpdatePlayerState();
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
            // Rail-riding logic
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

    #region Movement Logic

    private void ProcessGroundCheck()
    {
        grounded = Physics.Raycast(transform.position, Vector3.down,
                                   playerHeight * 0.5f + 0.3f, whatIsGround);
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
        if (!enableHighFallRoll) return;

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

    private void ProcessJumpInput()
    {
        if (wantJump && readyToJump &&
            (grounded || jumpCount < maxJumpCount || isWallRunning || isOnRail))
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
                Jump();
                jumpCount++;
            }
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        // Reset the AI jump input so we don't keep spamming
        wantJump = false;
    }

    private void ProcessCrouchAndGroundPoundInput()
    {
        if (wantCrouch)
        {
            if (!grounded)
                GroundPound();
            else
            {
                // Possibly do a slide if sprinting
                if (wantSprint && grounded && !isSliding)
                    Slide();
                else
                    StartCrouching();
            }
        }
        else
        {
            // If sliding, end slide
            if (isSliding)
                EndSlide();
            else if (isCrouching && CanStand())
                StopCrouching();
        }
        // Reset AI crouch input
        wantCrouch = false;
    }

    private void ProcessDashInput()
    {
        if (wantDash && canDash && !isCrouching)
            Dash();
        wantDash = false;
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
        else if (wantSprint && grounded)
            currentWalkSpeed = sprintSpeed;
        else
            currentWalkSpeed = moveSpeed;
    }

    private void UpdatePlayerState()
    {
        if (isRolling)
        {
            currentState = AIPlayerState.Rolling;
            return;
        }
        if (isOnRail)
            currentState = AIPlayerState.RailRiding;
        else if (isSliding)
            currentState = AIPlayerState.Sliding;
        else if (isWallRunning)
            currentState = AIPlayerState.WallRunning;
        else if (isDashing)
            currentState = AIPlayerState.Dashing;
        else if (!grounded)
            currentState = AIPlayerState.Jumping;
        else if (isCrouching)
            currentState = AIPlayerState.Crouching;
        else if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            currentState = (wantSprint && grounded) ? AIPlayerState.Sprinting : AIPlayerState.Walking;
        else
            currentState = AIPlayerState.Idle;
    }

    private float CurrentRollFovOffset
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

    #endregion

    #region Core Movement

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
        Vector3 spherePos = transform.position + originalColliderCenter + Vector3.up * (originalColliderHeight * 0.5f);
        Collider[] hits = Physics.OverlapSphere(spherePos, sphereRadius, whatIsGround);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject) return false;
        }
        return true;
    }

    private void Dash()
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

    private void Slide()
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

    #endregion

    #region Wall Run Logic

    private void CheckForWallRun()
    {
        RaycastHit hit;
        // Check left
        if (Physics.Raycast(transform.position, -transform.right, out hit, wallRunRayDistance))
        {
            if (hit.normal.y < 0.2f && hit.collider.CompareTag("WallRun"))
            {
                StartWallRun(hit.normal);
                return;
            }
        }
        // Check right
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

    #endregion

    #region Other

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

    private void GroundPound()
    {
        if (isGroundPounding) return;
        isGroundPounding = true;
        rb.AddForce(Vector3.down * groundPoundForce, ForceMode.VelocityChange);
        Debug.Log($"{gameObject.name}: Ground Pound triggered!");
    }

    private void StartRoll()
    {
        if (isRolling) return;
        float fallDuration = Time.time - fallStartTime;
        isRolling = true;
        currentState = AIPlayerState.Rolling;
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
