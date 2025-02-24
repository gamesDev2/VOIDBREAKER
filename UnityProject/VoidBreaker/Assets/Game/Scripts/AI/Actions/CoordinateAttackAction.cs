using UnityEngine;
using System.Linq; // For .Any() if you want

[CreateAssetMenu(menuName = "GoalActions/CoordinateAttack")]
public class CoordinateAttackActionSO : GoalAction
{
    public float allySupportDistance = 10f; // how close an ally needs to be
    public float moveSpeedDuringAttack = 3f;

    private void OnEnable()
    {
        effects.Clear();
        effects.Add("coordinatedAttack");
    }

    public override void ResetAction()
    {
        // No special reset in this example
    }

    public override bool IsDone()
    {
        // If you want this to be continuous, always return false
        return false;
    }

    public override bool CheckProceduralPrecondition(AI_Agent agent)
    {
        // 1) Must have a player
        if (agent.player == null)
        {
            Debug.LogWarning($"{agent.name}: CoordinateAttackAction precondition failed (no player).");
            return false;
        }

        // 2) Must have at least one ally in range
        bool hasAllyNearby = false;
        foreach (var ally in agent.knownAllies)
        {
            if (ally == null) continue;
            float dist = Vector3.Distance(agent.transform.position, ally.transform.position);
            if (dist <= allySupportDistance)
            {
                hasAllyNearby = true;
                break;
            }
        }

        if (!hasAllyNearby)
        {
            Debug.LogWarning($"{agent.name}: No allies within {allySupportDistance}m, can't do a coordinated attack.");
            return false;
        }

        return true;
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
        if (distance > allySupportDistance)
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
