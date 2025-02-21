using System.Collections.Generic;
using UnityEngine;

public class AI_Director : MonoBehaviour
{
    [Header("Agents Managed by the Director")]
    public List<GoapAgent> agents;

    [Header("Director Goals")]
    public List<GoapGoal> directorGoals;// this is a list of all possible goals the director can assign to agents

    public Transform player;
    public Transform baseLocation; // The location the agents are trying to protect
    public float playerThreatDistance = 15f;

    void Update()
    {
        // 1) Determine which director goal is best
        var bestDirectorGoal = GetBestDirectorGoal();
        if (bestDirectorGoal == null) return;

        // 2) Assign that goal to each agent
        foreach (var agent in agents)
        {
            // Agent will check this in ChooseBestGoal()
            agent.currentDirectorGoal = bestDirectorGoal;
        }
    }

    private GoapGoal GetBestDirectorGoal()
    {
        float distanceToPlayer = Vector3.Distance(player.position, baseLocation.position);
        GoapGoal bestGoal = null;
        float bestPriority = 0f;

        foreach (var g in directorGoals)
        {
            // If the player is close, we only want to assign the AttackPlayer goal
            bool isPlayerClose = distanceToPlayer < playerThreatDistance;
            bool isDefendBaseGoal = g.goalName == "DefendBase" && !isPlayerClose;
            bool isAttackPlayerGoal = g.goalName == "AttackPlayer" && isPlayerClose;

            if ((isAttackPlayerGoal || isDefendBaseGoal) && g.priority > bestPriority)
            {
                bestGoal = g;
                bestPriority = g.priority;
            }
        }

        return bestGoal;
    }
}
