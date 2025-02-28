using UnityEngine;

public class AI_Movement_Controller : EntityMovementController
{
    // AI-specific rotation input.
    private float aiLookYaw;

    protected override void ProcessInput()
    {
        // For AI, input is provided externally via SetAIInput.
        // Apply the AI's desired rotation.
        transform.Rotate(Vector3.up * aiLookYaw);
    }

    /// <summary>
    /// Sets the AI movement inputs.
    /// </summary>
    /// <param name="horizontal">Horizontal movement value.</param>
    /// <param name="vertical">Vertical movement value.</param>
    /// <param name="wantSprint">Sprint flag.</param>
    /// <param name="wantCrouch">Crouch flag.</param>
    /// <param name="wantJump">Jump flag.</param>
    /// <param name="wantDash">Dash flag.</param>
    /// <param name="lookYaw">Rotation (yaw) value for AI look.</param>
    public void SetAIInput(float horizontal, float vertical, bool wantSprint, bool wantCrouch, bool wantJump, bool wantDash, float lookYaw)
    {
        horizontalInput = horizontal;
        verticalInput = vertical;
        this.wantSprint = wantSprint;
        this.wantCrouch = wantCrouch;
        this.wantJump = wantJump;
        this.wantDash = wantDash;
        aiLookYaw = lookYaw;
    }
}
