using UnityEngine;

/// <summary>
/// An action that makes the agent attack the player.
/// </summary>
[CreateAssetMenu(menuName = "GoalActions/AttackPlayer")]
public class AttackPlayerAction : GoalAction
{
    private void OnEnable()
    {
        // This action satisfies the "attackPlayer" goal
        effects.Clear();
        effects.Add("attackPlayer");
    }

    public override void ResetAction() { }

    public override bool IsDone()
    {
        // Attacking can be continuous
        return false;
    }

    public override bool CheckProceduralPrecondition(AI_Agent agent)
    {
        return (agent != null && agent.player != null);
    }

    public override bool Perform(AI_Agent agent)
    {
        if (agent == null || agent.player == null)
            return false;

        // Example: log an attack
        Debug.Log($"{agent.name} is attacking {agent.player.name}!");
        return true;
    }
}
