using UnityEngine;

[CreateAssetMenu(menuName = "GoalActions/FollowPlayer")]
public class FollowPlayerActionSO : GoalAction
{
    // How close we want to get before stopping
    public float stopDistance = 2f;

    private void OnEnable()
    {
        // The effect this action accomplishes
        effects.Clear();
        effects.Add("following");
    }

    public override void ResetAction() { }
    public override bool IsDone() { return false; }  // Continuous follow

    public override bool CheckProceduralPrecondition(AI_Agent agent)
    {
        // Must have a valid player
        return (agent.player != null);
    }

    public override bool Perform(AI_Agent agent)
    {
        if (agent.player == null) return false;

        // Grab the AI_FPS_Controller
        AI_Movement_Controller controller = agent.GetComponent<AI_Movement_Controller>();
        if (controller == null)
        {
            Debug.LogWarning($"{agent.name}: Missing AI_FPS_Controller component.");
            return false;
        }

        // Compute direction to player
        Vector3 toPlayer = agent.player.position - agent.transform.position;
        float distance = toPlayer.magnitude;

        // If we're farther than stopDistance, move forward
        if (distance > stopDistance)
        {
            // We'll compute a simple yaw rotation to face the player.
            
            Vector3 forward = agent.transform.forward;
            float angle = Vector3.SignedAngle(forward, toPlayer.normalized, Vector3.up);

            // For simplicity, we directly rotate by that angle this frame
            float lookYaw = angle;

            // Move forward (vertical=1). No sprint/crouch/jump/dash by default.
            controller.SetAIInput(
                horizontal: 0f,
                vertical: 1f,
                wantSprint: false,
                wantCrouch: false,
                wantJump: false,
                wantDash: false,
                lookYaw: lookYaw
            );
        }
        else
        {
            // Already close enough: stop moving & stop turning
            controller.SetAIInput(
                horizontal: 0f,
                vertical: 0f,
                wantSprint: false,
                wantCrouch: false,
                wantJump: false,
                wantDash: false,
                lookYaw: 0f
            );
        }

        return true;
    }
}
