using System;
using Unity.VisualScripting;
using UnityEngine;

public enum EntityState
{
    Idle,
    Walking,
    Sprinting,
    Crouching,
    Dashing,
    Falling,
    Jumping,
    WallRunning,
    Sliding,
    RailRiding,
    Rolling
}
//we want a interface that takes our existing Health class makes it so the entity can take damage

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public abstract class Entity : MonoBehaviour
{
    //=== Shared Configuration Fields ===
    [Header("Entity Stats")]
    public float MaxHealth = 100;
    protected float CurrentHealth = 100;
    public float MaxEnergy = 100;
    protected float CurrentEnergy = 100;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float groundDrag = 6f;
    public float jumpForce = 5f;
    public float jumpCooldown = 0.25f;
    public float airMultiplier = 0.5f;
    public int maxJumpCount = 2;  // For double jump, set to 2

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
    public float maxSpeed = 10f;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;

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

    [Header("Roll & Camera Settings")]
    public float baseFov = 90f;
    public float maxFovIncrease = 20f;
    public float fovMultiplier = 10f;
    public float maxCameraRollAngle = 20f;
    public float rollMultiplier = 10f;

    [Header("Non-Wall Runnable Push Settings")]
    public float pushForce = 10f;
    public float sphereRadiusMultiplier = 1.2f;
    public float pushHeightThreshold = 2f;
    public float pushCooldown = 1f;

    [Header("Ground Pound Settings")]
    public float groundPoundForce = 20f;

    [Header("Performance Settings")]
    public float maxDeltaTime = 0.016f; // ~60 fps

    //=== Shared Components & State ===
    protected Rigidbody rb;
    protected CapsuleCollider capsuleCollider;

    // --- Missing fields now declared ---
    protected float originalColliderHeight;
    protected Vector3 originalColliderCenter;
    protected float originalMoveSpeed;

    // Input (to be set by derived classes)
    protected float horizontalInput;
    protected float verticalInput;
    protected bool wantSprint;
    protected bool wantCrouch;
    protected bool wantJump;
    protected bool wantDash;

    // Movement state
    protected bool grounded;
    protected int jumpCount = 0;
    protected bool readyToJump = true;
    protected bool isCrouching = false;
    protected bool isDashing = false;
    protected bool isSliding = false;
    protected bool isWallRunning = false;
    protected bool isOnRail = false;
    protected bool isRolling = false;
    protected bool isGroundPounding = false;

    protected float currentWalkSpeed;
    protected EntityState currentState = EntityState.Idle;
    public EntityState CurrentState { get { return currentState; } }

    protected float currentJumpSpeed;

    // Wall run variables
    protected float wallRunTimer = 0f;
    protected Vector3 currentWallNormal;

    // Rail riding
    protected Rail currentRail;

    // Fall roll variables
    protected float fallStartHeight;
    protected float fallStartTime;
    protected bool fallStarted = false;
    protected float rollEffectStartTime;
    protected float targetCameraFovIncrease;
    protected float targetCameraRollAngle;

    // Push cooldown
    protected float lastPushTime = 0f;

    // Rotation variables
    private float desiredXRotation = 0f;

    // Temporal Control
    private float timeMultiplier = 1.0f;

    //energy related variables
    [Header("Energy Settings")]
    private bool specialModeActive = false;
    public float specialModeEnergyDrain = 20f;
    private float timeSinceUsingEnergy = 0f;
    public float energyRegenRate = 40f; // Energy regen per second(the higher the value, the faster it regenerates)
    public float energyRegenDelay = 4f;  // Must wait given seconds of no usage to regen
    public float wallRunEnergyDrain = 7f;
    public float sprintEnergyDrain = 5f; // Energy drain per second while sprinting
    public float dashEnergyCost = 20f; // Energy cost for dashing

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
            Debug.LogError("No CapsuleCollider found on " + gameObject.name);
        originalColliderHeight = capsuleCollider.height;
        originalColliderCenter = capsuleCollider.center;
        originalMoveSpeed = moveSpeed;
        CurrentHealth = MaxHealth;
        CurrentEnergy = MaxEnergy;
    }

    protected virtual void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDeltaTime);
        ProcessGroundCheck();
        ProcessFallRoll(dt);
        ProcessInput();
        ProcessJumpInput();
        ProcessCrouchAndGroundPoundInput();
        ProcessDashInput();
        ProcessWallRunCheck(dt);
        SmoothCollider(dt);
        UpdateCurrentWalkSpeed();
        UpdatePlayerState();
        UpdateCameraController();
        HandleEnergy(dt);
    }
    protected virtual void HandleEnergy(float dt)
    {
        bool usedEnergyThisFrame = false;

        // Sprinting – continuous drain
        if (currentState == EntityState.Sprinting)
        {
            if (CurrentEnergy > 0f)
            {
                usedEnergyThisFrame = true;
                float drain = sprintEnergyDrain * dt;
                CurrentEnergy = Mathf.Max(CurrentEnergy - drain, 0f);
                // If we run out mid-sprint, forcibly stop sprint
                if (CurrentEnergy <= 0f)
                    wantSprint = false;
            }
            else
            {
                // If energy is 0, we can’t sprint
                wantSprint = false;
            }
        }

        // Wallrunning – continuous drain
        if (currentState == EntityState.WallRunning)
        {
            if (CurrentEnergy > 0f)
            {
                usedEnergyThisFrame = true;
                float drain = wallRunEnergyDrain * dt;
                CurrentEnergy = Mathf.Max(CurrentEnergy - drain, 0f);
                if (CurrentEnergy <= 0f)
                {
                    // Force end wallrun if energy is gone
                    EndWallRun();
                }
            }
        }

        // Special Mode – continuous drain
        if (specialModeActive)
        {
            if (CurrentEnergy > 0f)
            {
                usedEnergyThisFrame = true;
                // Use Time.unscaledDeltaTime so that the drain is not affected by timeScale.
                float drain = specialModeEnergyDrain * Time.unscaledDeltaTime;
                CurrentEnergy = Mathf.Max(CurrentEnergy - drain, 0f);
                // If energy has run out, force exit from blade mode.
                if (CurrentEnergy <= 0f)
                {
                    specialModeActive = false;
                }
            }
            else
            {
                specialModeActive = false;
            }
        }


        if (usedEnergyThisFrame)
        {
            timeSinceUsingEnergy = 0f;
        }
        else
        {
            // We didn’t do anything that used energy
            timeSinceUsingEnergy += dt;
            // Regenerate if 2s have passed since last usage
            if (timeSinceUsingEnergy >= energyRegenDelay && CurrentEnergy < MaxEnergy)
            {
                CurrentEnergy = Mathf.Min(CurrentEnergy + energyRegenRate * dt, MaxEnergy);
            }
        }
        OnEnergyChanged(CurrentEnergy);
    }


    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
            Die();
    }
    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
    }
    public float GetHealth() { return CurrentHealth; }
    public float GetEnergy() { return CurrentEnergy; }
    protected abstract void Die();
    protected virtual void OnHealthChanged(float newHealth) { }
    protected virtual void OnEnergyChanged(float newEnergy) { }

    public bool SetSpecialModeActive(bool active)
    {
        specialModeActive = active;
        return specialModeActive;
    }
    public bool IsSpecialModeActive()
    {
        return specialModeActive;
    }
    protected virtual void FixedUpdate()
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
        rb.MoveRotation(Quaternion.Euler(Vector3.up * desiredXRotation));
        rb.AddForce(Physics.gravity * ((timeMultiplier - 1.0f) * 10.0f), ForceMode.Acceleration);
    }

    //--- Abstract Input Handling ---
    protected abstract void ProcessInput();

    //--- Common Functionality Methods ---

    protected virtual void ProcessGroundCheck()
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

    protected virtual void ProcessFallRoll(float dt)
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

    protected virtual void ProcessJumpInput()
    {
        if (InputJump() && readyToJump && (grounded || jumpCount < maxJumpCount || isWallRunning || isOnRail))
        {
            if (isOnRail)
                ExitRail();
            if (isCrouching && CanStand())
                StopCrouching();
            if (isWallRunning)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
                rb.AddForce((Vector3.up + currentWallNormal) * jumpForce * timeMultiplier, ForceMode.Impulse);
                EndWallRun();
                jumpCount = 1;
            }
            else
            {
                Jump();
                jumpCount++;
            }
            readyToJump = false;
            Invoke(nameof(ResetJump), jumpCooldown * (1.0f / timeMultiplier));
        }
        ResetInputJump();
    }

    protected virtual bool InputJump() { return wantJump; }
    protected virtual void ResetInputJump() { wantJump = false; }

    protected virtual void ProcessCrouchAndGroundPoundInput()
    {
        if (InputCrouch())
        {
            if (!grounded)
                GroundPound();
            else
            {
                if (InputSprint() && grounded && !isSliding)
                    Slide();
                else
                    StartCrouching();
            }
        }
        else
        {
            if (isSliding)
                EndSlide();
            else if (isCrouching && CanStand())
                StopCrouching();
        }
        ResetInputCrouch();
    }

    protected virtual bool InputCrouch() { return wantCrouch; }
    protected virtual void ResetInputCrouch() { wantCrouch = false; }

    protected virtual void ProcessDashInput()
    {
        if (InputDash() && CanDash() && !isCrouching)
            Dash();
        ResetInputDash();
    }

    protected virtual bool InputDash() { return wantDash; }
    protected virtual void ResetInputDash() { wantDash = false; }
    protected virtual bool CanDash() { return true; }

    protected virtual void ProcessWallRunCheck(float dt)
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

    protected virtual void SmoothCollider(float dt)
    {
        float targetHeight = isCrouching ? crouchHeight : originalColliderHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchTransitionSpeed * dt);
        Vector3 targetCenter = originalColliderCenter;
        targetCenter.y -= (originalColliderHeight - targetHeight) / 2f;
        capsuleCollider.center = Vector3.Lerp(capsuleCollider.center, targetCenter, crouchTransitionSpeed * dt);
    }

    protected virtual void UpdateCurrentWalkSpeed()
    {
        if (isOnRail && currentRail != null)
            currentWalkSpeed = currentRail.railSpeed * timeMultiplier;
        else if (isDashing)
            currentWalkSpeed = maxSpeed * timeMultiplier;
        else if (isSliding)
            currentWalkSpeed = slideSpeed * timeMultiplier;
        else if (isCrouching)
            currentWalkSpeed = crouchSpeed * timeMultiplier;
        else if (InputSprint() && grounded)
            currentWalkSpeed = sprintSpeed * timeMultiplier;
        else if (currentState == EntityState.Idle)
            currentWalkSpeed = Mathf.Lerp(currentWalkSpeed, 0.0f, Time.deltaTime * timeMultiplier);
        else
            currentWalkSpeed = moveSpeed * timeMultiplier;
    }

    protected virtual bool InputSprint() { return wantSprint; }

    protected virtual void UpdatePlayerState()
    {
        if (isRolling)
        {
            currentState = EntityState.Rolling;
            return;
        }
        if (isOnRail)
            currentState = EntityState.RailRiding;
        else if (isSliding)
            currentState = EntityState.Sliding;
        else if (isWallRunning)
            currentState = EntityState.WallRunning;
        else if (isDashing)
            currentState = EntityState.Dashing;
        else if (!grounded)
            currentState = EntityState.Jumping;
        else if (isCrouching)
            currentState = EntityState.Crouching;
        else if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(verticalInput) > 0.1f)
            currentState = (InputSprint() && grounded) ? EntityState.Sprinting : EntityState.Walking;
        else
            currentState = EntityState.Idle;
    }

    protected virtual void UpdateCameraController()
    {
        // Base implementation does nothing.
        // Derived classes (e.g., FPS_Controller) should override.
    }

    //--- Movement Methods ---
    protected virtual void MovePlayer()
    {
        Vector3 moveDir = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (grounded)
            rb.AddForce(moveDir * currentWalkSpeed * 10f * timeMultiplier, ForceMode.Force);
        else
            rb.AddForce(moveDir * currentWalkSpeed * 10f * airMultiplier * timeMultiplier, ForceMode.Force);
        SpeedControl();
    }

    protected virtual void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > currentWalkSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentWalkSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }
    }

    protected virtual void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(Vector3.up * jumpForce * timeMultiplier, ForceMode.Impulse);
    }

    protected virtual void ResetJump()
    {
        readyToJump = true;
    }

    protected virtual void StartCrouching()
    {
        isCrouching = true;
    }

    protected virtual void StopCrouching()
    {
        isCrouching = false;
    }

    protected virtual bool CanStand()
    {
        float sphereRadius = capsuleCollider.radius * 0.9f;
        Vector3 spherePos = transform.position + Vector3.up * (playerHeight * 0.5f);
        Collider[] hits = Physics.OverlapSphere(spherePos, sphereRadius, whatIsGround);
        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject)
                return false;
        }
        return true;
    }

    protected virtual void Dash()
    {
        // Check if we have enough energy to dash:
        if (CurrentEnergy < dashEnergyCost)
        {
            // Not enough energy => cannot dash
            return;
        }

        // Subtract dash cost
        CurrentEnergy = Mathf.Max(CurrentEnergy - dashEnergyCost, 0f);

        // Continue with your normal dash logic:
        isDashing = true;
        Vector3 dashDir = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (dashDir == Vector3.zero)
            dashDir = transform.forward;
        rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

        Invoke(nameof(EndDash), dashDuration);
        Invoke(nameof(ResetDash), dashCooldown);

        // Because we used energy, reset timer:
        timeSinceUsingEnergy = 0f;
    }
    protected virtual void EndDash()
    {
        isDashing = false;
    }

    protected virtual void ResetDash()
    {
        // Optional additional logic.
    }

    protected virtual void Slide()
    {
        isSliding = true;
        StartCrouching();
        Vector3 slideDir = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;
        if (slideDir == Vector3.zero)
            slideDir = transform.forward;
        rb.AddForce(slideDir * dashForce, ForceMode.Impulse);
    }

    protected virtual void EndSlide()
    {
        isSliding = false;
        Invoke(nameof(ResetDash), dashCooldown);
    }

    //--- Rail Riding Methods ---
    public virtual void EnterRail(Rail rail)
    {
        isOnRail = true;
        currentRail = rail;
        rb.useGravity = false;
        Vector3 railPoint = currentRail.GetClosestPointOnRail(rb.position);
        railPoint.y += railVerticalOffset;
        rb.position = new Vector3(rb.position.x, railPoint.y, rb.position.z);
    }

    public virtual void ExitRail()
    {
        isOnRail = false;
        currentRail = null;
        rb.useGravity = true;
    }

    protected virtual void HandleRailMovement()
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

    //--- Wall Run Methods ---
    protected virtual void CheckForWallRun()
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

    protected virtual void StartWallRun(Vector3 wallNormal)
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

    protected virtual void EndWallRun()
    {
        isWallRunning = false;
        wallRunTimer = 0f;
        rb.useGravity = true;
    }

    protected virtual bool IsWallStillValid()
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

    protected virtual void WallRunMovement()
    {
        float gravityMultiplier = Mathf.Lerp(0f, 1f, wallRunTimer / maxWallRunTime);
        rb.AddForce(Vector3.up * Physics.gravity.y * wallRunGravity * gravityMultiplier * (10.0f * (timeMultiplier - 1.0f)), ForceMode.Acceleration);
        Vector3 wallTangent = Vector3.Cross(currentWallNormal, Vector3.up).normalized;
        if (Vector3.Dot(wallTangent, transform.forward) < 0)
            wallTangent = -wallTangent;
        Vector3 currentTangentVel = Vector3.Project(rb.velocity, wallTangent);
        if (currentTangentVel.magnitude < maxWallRunSpeed)
            rb.AddForce(wallTangent * wallRunAcceleration * timeMultiplier, ForceMode.Acceleration);
    }

    //--- Other Utilities ---
    protected virtual void CheckForNonWallRunnableCollision()
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

    protected virtual void GroundPound()
    {
        if (isGroundPounding)
            return;
        isGroundPounding = true;
        rb.AddForce(Vector3.down * groundPoundForce, ForceMode.VelocityChange);
    }

    public virtual float CurrentRollFovOffset
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

    protected virtual void StartRoll()
    {
        if (isRolling)
            return;
        float fallDuration = Time.time - fallStartTime;
        targetCameraFovIncrease = Mathf.Clamp(fallDuration * fovMultiplier, 0f, maxFovIncrease);
        targetCameraRollAngle = Mathf.Clamp(fallDuration * rollMultiplier, 0f, maxCameraRollAngle);
        isRolling = true;
        currentState = EntityState.Rolling;
        rollEffectStartTime = Time.time;
        rb.AddForce(transform.forward * rollForce, ForceMode.Impulse);
        Invoke(nameof(EndRoll), rollDuration);
    }

    protected virtual void EndRoll()
    {
        isRolling = false;
    }

    protected void deltaRotX(float delta)
    {
        desiredXRotation += delta;
    }

    protected float getRotX()
    {
        return desiredXRotation;
    }

    public float timeFlow
    {
        get { return timeMultiplier; }
        set 
        {
            rb.velocity *= 1 / timeMultiplier;
            timeMultiplier = value;
        }
    }

}
