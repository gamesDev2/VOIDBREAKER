using UnityEngine;

public class Player_Controller : Entity
{
    [Header("FPS Specific Settings")]
    [Tooltip("Assign the player's head transform (child of the player).")]
    public Transform head;
    [Tooltip("Assign the player's Camera.")]
    public Camera playerCamera;
    [Tooltip("Mouse sensitivity for camera look.")]
    public float mouseSensitivity = 100f;
    public float verticleSensitivity = 300f;

    [Header("Input Keys")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode dashKey = KeyCode.E;
    public KeyCode sprintKey = KeyCode.LeftShift;

    // Roll & camera settings are inherited from EntityMovementController:
    // maxFovIncrease, fovMultiplier, maxCameraRollAngle, rollMultiplier,
    // as well as crouchCameraHeight.

    private Vector3 standHeadLocalPos;
    private Vector3 crouchHeadLocalPos;
    private Camera_Controller camCtrl;

    protected override void Awake()
    {
        base.Awake();
        if (head != null)
        {
            standHeadLocalPos = head.localPosition;
            crouchHeadLocalPos = new Vector3(standHeadLocalPos.x, standHeadLocalPos.y - crouchCameraHeight, standHeadLocalPos.z);
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        camCtrl = playerCamera.GetComponent<Camera_Controller>();

        Game_Manager.Instance.player = gameObject; // Store the reference to the player.

    }

    protected override void ProcessInput()
    {
        // Mouse look (horizontal rotation)
        
        
        //transform.Rotate(Vector3.up * mouseX);
        

        // Movement input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Action keys
        wantSprint = Input.GetKey(sprintKey);
        wantCrouch = Input.GetKey(crouchKey);
        if (Input.GetKeyDown(jumpKey))
            wantJump = true;
        if (Input.GetKeyDown(dashKey) && !isDashing)
            wantDash = true;
    }

    // Let the Camera_Controller manage FOV transitions.
    protected override void UpdateCameraController()
    {
        if (playerCamera != null)
        {
            Camera_Controller camCtrl = playerCamera.GetComponent<Camera_Controller>();
            if (camCtrl != null)
            {
                // Pass the current movement state and roll offset.
                camCtrl.SetPlayerState(currentState);
                camCtrl.SetRollFovOffset(CurrentRollFovOffset);
            }
        }
    }

    private void LateUpdate()
    {
        // Smoothly adjust the head (and therefore camera) position for crouch.
        if (head != null)
        {
            head.localPosition = Vector3.Lerp(head.localPosition,
                isCrouching ? crouchHeadLocalPos : standHeadLocalPos,
                crouchTransitionSpeed * Time.deltaTime);
        }

        //based on the player's current state and velocity, update the speedline opacity
        if (playerCamera != null)
        {
            playerCamera.transform.position = transform.position + head.localPosition;

            if (camCtrl != null && rb != null)
            {

                float speed = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
                float speedNormalized = Mathf.Clamp01((speed / maxSpeed) * 3f);
                camCtrl.setSpeedlineOpacity(speedNormalized);
                

                float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * timeFlow;
                float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime * timeFlow;

                deltaRotX(mouseX);

                camCtrl.yRot -= mouseY;
                camCtrl.xRot += mouseX;

                camCtrl.timeFlow = timeFlow;
            }
        }
    }
    protected override void Die()
    {
        Debug.Log("Player has died!");
    }
}
