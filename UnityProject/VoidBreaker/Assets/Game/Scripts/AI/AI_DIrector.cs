using System.Collections.Generic;
using UnityEngine;

public class AIDirectorGoap : MonoBehaviour
{
    [Header("Agents Under Director Control")]
    public List<AI_Agent> agents;

    [Header("Player Reference")]
    public GameObject player;

    [Header("Director Logic")]
    public float groupAttackDistance = 15f;
    public bool forceGroupAttack; // For debug toggling in the Inspector

    void Awake()
    {
        // For each agent, create a list of all the other agents (i.e., "allies").
        for (int i = 0; i < agents.Count; i++)
        {
            // Copy the full list
            List<AI_Agent> allyList = new List<AI_Agent>(agents);
            // Remove the agent itself from the allyList
            allyList.RemoveAt(i);
            // Assign to that agent
            agents[i].knownAllies = allyList;
        }
    }

    void Update()
    {
        // If the player isn't valid or doesn't have the "Player" tag, do nothing.
        if (player == null || !player.CompareTag("Player"))
            return;

        // For each agent, decide what goal to assign.
        foreach (var agent in agents)
        {
            if (agent == null) continue;

            // Ensure the agent knows who to follow/attack
            agent.player = player.transform;

            // assign a "coordinatedAttack" goal. Otherwise, "following".
            float distance = Vector3.Distance(agent.transform.position, player.transform.position);

            if (forceGroupAttack || distance < groupAttackDistance)
            {
                SetAgentGoal(agent, "coordinatedAttack");
            }
            else
            {
                SetAgentGoal(agent, "following");
            }
        }
    }

    private void SetAgentGoal(AI_Agent agent, string goalKey) // Helper method to set a goal for an agent
    {
        agent.goal.Clear();
        agent.goal.Add(goalKey, true);
    }
}
