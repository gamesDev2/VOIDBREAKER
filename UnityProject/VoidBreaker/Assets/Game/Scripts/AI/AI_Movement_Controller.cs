using UnityEngine;

public class AI_Movement_Controller : Entity
{
    [SerializeField] private float rotationSpeed = 360f;
    private float aiLookYaw;

    protected override void ProcessInput()
    {
        // Smoothly rotate toward the desired yaw
        Quaternion currentRot = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0f, aiLookYaw, 0f);
        transform.rotation = Quaternion.RotateTowards(
            currentRot, targetRot, rotationSpeed * Time.deltaTime);
        // Movement is handled in Entity.MovePlayer via horizontal/vertical input
    }

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
        aiLookYaw = lookYaw;
    }

    protected override void Die()
    {
        Debug.Log("AI " + gameObject.name + " has died!");
    }
}
