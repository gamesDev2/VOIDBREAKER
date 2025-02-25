using UnityEngine;

[CreateAssetMenu(menuName = "GoalActions/CoordinatedAttack")]
public class CoordinatedAttackAction : GoalAction
{
    [Header("Attack Settings")]
    [Tooltip("Distance within which the AI can attack the player.")]
    public float attackRange = 3f;

    [Tooltip("If the AI is farther than this from the player, it may sprint.")]
    public float sprintDistance = 10f;

    [Header("Ally Coordination")]
    [Tooltip("How close an ally must be to consider this a 'coordinated' attack.")]
    public float allySupportDistance = 8f;

    [Tooltip("If true, requires at least one ally also targeting 'coordinatedAttack' within allySupportDistance.")]
    public bool requireAllySupport = true;

    private void OnEnable()
    {
        // This action satisfies the "coordinatedAttack" goal
        effects.Clear();
        effects.Add("coordinatedAttack");
    }

    /// <summary>
    /// Checks if we can do a coordinated attack:
    /// - The agent must have a valid player.
    /// - (Optional) At least one ally is also going for "coordinatedAttack" and is within allySupportDistance.
    /// </summary>
    public override bool CheckProceduralPrecondition(AI_Agent agent)
    {
        if (agent.player == null) return false;

        // If we require ally support, verify there's an ally in range
        if (requireAllySupport)
        {
            bool hasAlly = false;
            foreach (var ally in agent.knownAllies)
            {
                if (ally == null) continue;
                // Check if ally's goal is also "coordinatedAttack"
                if (ally.goal.ContainsKey("coordinatedAttack") && ally.goal["coordinatedAttack"])
                {
                    float dist = Vector3.Distance(agent.transform.position, ally.transform.position);
                    if (dist <= allySupportDistance)
                    {
                        hasAlly = true;
                        break;
                    }
                }
            }
            if (!hasAlly) return false;
        }

        return true;
    }

    /// <summary>
    /// Called once when the planner picks this action. Reset any internal state if needed.
    /// </summary>
    public override bool ResetAction()
    {
        // No internal state to reset in this example
        return true;
    }

    /// <summary>
    /// Returns whether the action is complete.
    /// Since we want a continuous "coordinated attack," we can keep it going indefinitely.
    /// </summary>
    public override bool IsDone()
    {
        return false;
    }

    /// <summary>
    /// Called every frame while this action is active. Moves the AI into range and "attacks."
    /// </summary>
    public override bool Perform(AI_Agent agent)
    {
        if (agent.player == null) return false;

        // Use your advanced AI movement controller
        AI_Movement_Controller movement = agent.GetComponent<AI_Movement_Controller>();
        if (movement == null) return false;

        // Direction to player
        Vector3 toPlayer = agent.player.position - agent.transform.position;
        float distance = toPlayer.magnitude;

        // If out of attack range, move closer
        float verticalInput = (distance > attackRange) ? 1f : 0f;

        // Possibly sprint if far from the player
        bool wantSprint = (distance > sprintDistance);

        // We'll do a simple rotation to face the player
        float angle = Vector3.SignedAngle(agent.transform.forward, toPlayer.normalized, Vector3.up);

        // If within attack range, "attack" the player
        if (distance <= attackRange)
        {
            verticalInput = 0f; // stop moving forward
            // Insert your real attack logic here
            Debug.Log($"{agent.name} is COORDINATED-ATTACKING the player!");
        }

        // Set AI input
        movement.SetAIInput(
            horizontal: 0f,
            vertical: verticalInput,
            wantSprint: wantSprint,
            wantCrouch: false,
            wantJump: false,
            wantDash: false,
            lookYaw: angle
        );

        return true;
    }
}
